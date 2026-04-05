using System.Net;
using System.Text.Json;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Domain.Interfaces;
using BlockchainTracker.Infrastructure.Caching;
using BlockchainTracker.Infrastructure.Clients;
using BlockchainTracker.Infrastructure.Persistence;
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
        services.AddPooledDbContextFactory<BlockchainDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("PostgreSql")));

        services.AddScoped<IBlockchainSnapshotRepository, BlockchainSnapshotRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IBlockchainTrackerMetrics, BlockchainTrackerMetrics>();

        var baseUrl = configuration["BlockCypher:BaseUrl"]
            ?? throw new InvalidOperationException("BlockCypher:BaseUrl configuration is required but was not found.");

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            })
        };

        services.AddSingleton(sp => GetCircuitBreakerPolicy(sp.GetRequiredService<IBlockchainTrackerMetrics>()));

        services.AddRefitClient<IBlockCypherApi>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl))
            .AddPolicyHandler(GetRateLimitPolicy())
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler((sp, _) => sp.GetRequiredService<AsyncPolicy<HttpResponseMessage>>());

        services.AddSingleton<IBlockchainApiClient, BlockCypherApiClient>();

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }

    private static AsyncPolicy<HttpResponseMessage> GetRateLimitPolicy()
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: (retryAttempt, result, _) =>
                {
                    var baseDelay = result.Result?.Headers.RetryAfter?.Delta
                        ?? TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));

                    var jitter = TimeSpan.FromSeconds(1 + Random.Shared.NextDouble() * 2);

                    return baseDelay + jitter;
                },
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);
    }

    private static AsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static AsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IBlockchainTrackerMetrics metrics)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30),
                onBreak: (_, _) => metrics.RecordCircuitBreakerTrip("blockcypher"),
                onReset: () => { });
    }
}
