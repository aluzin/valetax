using Valetax.Domain.Entities;

namespace Valetax.Application.Abstractions;

public interface IExceptionJournalWriter
{
    Task WriteAsync(ExceptionJournal journal, CancellationToken cancellationToken);
}
