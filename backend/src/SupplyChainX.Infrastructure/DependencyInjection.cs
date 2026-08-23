using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Infrastructure.Persistence;

namespace SupplyChainX.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=supplychainx_db;Username=postgres;Password=postgres_dev_password";

        // Register Entity Framework Core DbContext with PostgreSQL Npgsql driver
        services.AddDbContext<SupplyChainXDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(SupplyChainXDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        // Register ISupplyChainXDbContext interface in DI container pointing to SupplyChainXDbContext
        services.AddScoped<ISupplyChainXDbContext>(provider => provider.GetRequiredService<SupplyChainXDbContext>());

        // Register EF Core DbContext Health Check for PostgreSQL database connectivity
        services.AddHealthChecks()
            .AddDbContextCheck<SupplyChainXDbContext>(
                name: "database",
                tags: new[] { "db", "postgresql" });

        return services;
    }
}
