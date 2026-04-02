using Valetax.Application.Abstractions;
using Valetax.Domain.Entities;

namespace Valetax.Infrastructure.Persistence.Services;

public sealed class ExceptionJournalWriter(ValetaxDbContext dbContext) : IExceptionJournalWriter
{
    public async Task WriteAsync(ExceptionJournal journal, CancellationToken cancellationToken)
    {
        dbContext.ExceptionJournals.Add(journal);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
