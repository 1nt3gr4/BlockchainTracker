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
        services.AddPooledDbContextFactory<BlockchainDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PostgreSql")));

        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBlockchainSnapshotRepository>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<BlockchainDbContext>>();
            var context = factory.CreateDbContext();
            return new BlockchainSnapshotRepository(context);
        });

        services.AddRefitClient<IBlockCypherApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.blockcypher.com"))
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddSingleton<IBlockchainApiClient, BlockCypherApiClient>();

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        services.AddSingleton<BlockchainTrackerMetrics>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
