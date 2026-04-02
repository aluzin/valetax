using Microsoft.EntityFrameworkCore;
using Valetax.Domain.Entities;

namespace Valetax.Application.Abstractions;

public interface IValetaxDbContext
{
    DbSet<ExceptionJournal> ExceptionJournals { get; }

    DbSet<Tree> Trees { get; }

    DbSet<Node> Nodes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
