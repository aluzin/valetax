namespace Valetax.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public string ConnectionString { get; set; } = null!;

    public bool MigrateOnStartup { get; set; }
}
