using BlockchainTracker.Api.Endpoints;
using BlockchainTracker.Api.HealthChecks;
using BlockchainTracker.Api.Workers;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.Configure<PollingSettings>(builder.Configuration.GetSection(PollingSettings.SectionName));
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection(CacheSettings.SectionName));
builder.Services.Configure<HealthCheckSettings>(builder.Configuration.GetSection(HealthCheckSettings.SectionName));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddCheck<BlockchainDataHealthCheck>("blockchain-data");

builder.Services.AddHostedService<BlockchainPollingWorker>();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.MapChainEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
