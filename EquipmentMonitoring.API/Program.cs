using EquipmentMonitoring.Application.Abstractions.Persistence;
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

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
