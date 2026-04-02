using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Valetax.Infrastructure.Persistence;

namespace Valetax.Infrastructure;

public static class ApplicationBuilderExtensions
{
    public static async Task InitializeInfrastructureAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetService<ValetaxDbContext>();
        var databaseOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        if (dbContext is null || !databaseOptions.MigrateOnStartup)
        {
            return;
        }

        var hasMigrations = dbContext.Database.GetMigrations();

        if (hasMigrations.Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
