using Microsoft.EntityFrameworkCore;
using Valetax.Application.Abstractions;
using Valetax.Application.Telemetry;
using Valetax.Domain.Exceptions;

namespace Valetax.Application.Journal.GetSingle;

public sealed class GetJournalSingleService(IValetaxDbContext dbContext) : IGetJournalSingleService
{
    public async Task<GetJournalSingleResult?> ExecuteAsync(GetJournalSingleRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationTracing.ActivitySource.StartActivity("journal.get-single");
        using var metrics = ApplicationMetrics.StartUseCase("journal.get-single");
        activity?.SetTag("journal.event_id", request?.Id);

        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var journal = await dbContext.ExceptionJournals
                .AsNoTracking()
                .Where(entry => entry.EventId == request.Id)
                .Select(entry => new GetJournalSingleResult
                {
                    Id = entry.Id,
                    EventId = entry.EventId,
                    CreatedAt = entry.CreatedAt,
                    Text = entry.Text
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (journal is null)
            {
                throw new SecureException("Journal event was not found");
            }

            activity?.SetTag("journal.found", true);

            return journal;
        }
        catch (Exception exception)
        {
            metrics.MarkFailure(exception);
            throw;
        }
    }
}
