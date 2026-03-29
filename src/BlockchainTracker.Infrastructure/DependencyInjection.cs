using System.Net;
using System.Text.Json;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Domain.Interfaces;
using BlockchainTracker.Infrastructure.Caching;
using BlockchainTracker.Infrastructure.Clients;
using BlockchainTracker.Infrastructure.Persistence;
using BlockchainTracker.Infrastructure.Services;
using BlockchainTracker.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Refit;

namespace BlockchainTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPooledDbContextFactory<BlockchainDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PostgreSql")));

        services.AddScoped<IBlockchainSnapshotRepository, BlockchainSnapshotRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<BlockchainTrackerMetrics>();

        var baseUrl = configuration["BlockCypher:BaseUrl"] ?? "https://api.blockcypher.com";

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            })
        };

        IAsyncPolicy<HttpResponseMessage>? circuitBreakerPolicy = null;

        services.AddRefitClient<IBlockCypherApi>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl))
            .AddPolicyHandler(GetRateLimitPolicy())
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler((sp, _) =>
            {
                return circuitBreakerPolicy ??= GetCircuitBreakerPolicy(
                    sp.GetRequiredService<BlockchainTrackerMetrics>());
            });

        services.AddSingleton<IBlockchainApiClient, BlockCypherApiClient>();

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        services.AddSingleton<IBlockchainDataFetcherService, BlockchainDataFetcherService>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRateLimitPolicy()
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: (retryAttempt, result, _) =>
                {
                    if (result.Result?.Headers.RetryAfter?.Delta is { } delta)
                        return delta;

                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                },
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(BlockchainTrackerMetrics metrics)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
                onBreak: (_, _) => metrics.RecordCircuitBreakerTrip("blockcypher"),
                onReset: () => { });
    }
}
