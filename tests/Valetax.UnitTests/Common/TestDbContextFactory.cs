using Microsoft.EntityFrameworkCore;

namespace Valetax.UnitTests.Common;

internal static class TestDbContextFactory
{
    public static TestDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .EnableSensitiveDataLogging()
            .Options;

        return new TestDbContext(options);
    }
}
