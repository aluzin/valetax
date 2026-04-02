using Microsoft.EntityFrameworkCore;
using Valetax.Application.Abstractions;
using Valetax.Domain.Entities;

namespace Valetax.UnitTests.Common;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options), IValetaxDbContext
{
    public DbSet<ExceptionJournal> ExceptionJournals => Set<ExceptionJournal>();

    public DbSet<Tree> Trees => Set<Tree>();

    public DbSet<Node> Nodes => Set<Node>();
}
