using System.Text.Json.Serialization;
using EquipmentMonitoring.API.OpenApi;
using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Infrastructure.Persistence;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("EquipmentDatabase")
    ?? throw new InvalidOperationException("Connection string 'EquipmentDatabase' is not configured.");

var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);

if (!string.IsNullOrWhiteSpace(sqliteConnectionStringBuilder.DataSource)
    && !Path.IsPathRooted(sqliteConnectionStringBuilder.DataSource)
    && sqliteConnectionStringBuilder.DataSource != ":memory:")
{
    sqliteConnectionStringBuilder.DataSource = Path.GetFullPath(
        sqliteConnectionStringBuilder.DataSource,
        builder.Environment.ContentRootPath);
}

var resolvedConnectionString = sqliteConnectionStringBuilder.ToString();

// Add services to the container.
builder.Services.AddDbContext<EquipmentMonitoringDbContext>(options =>
    options.UseSqlite(resolvedConnectionString));
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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<EquipmentOpenApiDocumentTransformer>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "EquipmentMonitoring API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
