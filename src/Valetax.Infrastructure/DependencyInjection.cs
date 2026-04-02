using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Valetax.Application.Abstractions;
using Valetax.Infrastructure.Authentication;
using Valetax.Infrastructure.Persistence;
using Valetax.Infrastructure.Persistence.Services;

namespace Valetax.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseSection = configuration.GetSection("Database");
        var connectionString = databaseSection[nameof(DatabaseOptions.ConnectionString)];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }
        
        var databaseOptions = new DatabaseOptions
        {
            ConnectionString = connectionString,
            MigrateOnStartup = bool.TryParse(
                databaseSection[nameof(DatabaseOptions.MigrateOnStartup)],
                out var migrateOnStartup) && migrateOnStartup
        };

        services.Configure<DatabaseOptions>(options =>
        {
            options.ConnectionString = databaseOptions.ConnectionString;
            options.MigrateOnStartup = databaseOptions.MigrateOnStartup;
        });

        services.AddDbContext<ValetaxDbContext>(options => options.UseNpgsql(databaseOptions.ConnectionString));
        services.AddScoped<IValetaxDbContext>(provider => provider.GetRequiredService<ValetaxDbContext>());
        services.AddScoped<IDbUpdateExceptionMapper, PostgresDbUpdateExceptionMapper>();
        services.AddScoped<IExceptionJournalWriter, ExceptionJournalWriter>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
