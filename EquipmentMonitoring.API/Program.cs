using System.Text.Json.Serialization;
using EquipmentMonitoring.API.OpenApi;
using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.Monitoring;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Infrastructure.Persistence;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("EquipmentDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'EquipmentDatabase' is not configured. " +
        "For Docker Compose run, use .env and docker-compose.yaml. " +
        "For local non-Docker run, set ConnectionStrings__EquipmentDatabase to a PostgreSQL connection string.");

builder.Services.AddDbContext<EquipmentMonitoringDbContext, PostgresEquipmentMonitoringDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsql => npgsql.MigrationsAssembly("EquipmentMonitoring.Infrastructure")));
builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
builder.Services.AddScoped<IMeasurementRepository, MeasurementRepository>();
builder.Services.AddScoped<IMaintenancePlanRepository, MaintenancePlanRepository>();
builder.Services.AddScoped<CreateEquipmentValidator>();
builder.Services.AddScoped<EquipmentRegistrationValidator>();
builder.Services.AddScoped<CreateMeasurementValidator>();
builder.Services.AddScoped<MeasurementRegistrationValidator>();
builder.Services.AddScoped<CreateMaintenancePlanValidator>();
builder.Services.AddScoped<MaintenancePlanRegistrationValidator>();
builder.Services.AddScoped<UpdateMaintenancePlanStatusValidator>();

builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddScoped<IThresholdRepository, ThresholdRepository>();
builder.Services.AddScoped<IWarningRepository, WarningRepository>();
builder.Services.AddScoped<UpsertThresholdValidator>();
builder.Services.AddScoped<IMeasurementRule, ThresholdBreachRule>();
builder.Services.AddScoped<IMeasurementMonitor, MeasurementMonitor>();
builder.Services.AddScoped<ThresholdService>();
builder.Services.AddScoped<WarningService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<EquipmentMonitoringDbContext>("database", tags: [ReadinessTag]);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<EquipmentOpenApiDocumentTransformer>();
    options.AddDocumentTransformer<MonitoringOpenApiDocumentTransformer>();
});

var app = builder.Build();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentMonitoringDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "EquipmentMonitoring API v1");
        options.RoutePrefix = "swagger";
    });
}

if (app.Configuration.GetValue("Http:UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadinessTag) });

app.Run();

public partial class Program
{
    private const string ReadinessTag = "ready";
}
