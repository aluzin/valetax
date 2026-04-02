using Microsoft.EntityFrameworkCore;
using Valetax.Application.Abstractions;
using Valetax.Application.Telemetry;
using Valetax.Domain.Exceptions;

namespace Valetax.Application.Journal.GetRange;

public sealed class GetJournalRangeService(IValetaxDbContext dbContext) : IGetJournalRangeService
{
    public async Task<GetJournalRangeResult> ExecuteAsync(GetJournalRangeRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationTracing.ActivitySource.StartActivity("journal.get-range");
        using var metrics = ApplicationMetrics.StartUseCase("journal.get-range");
        activity?.SetTag("journal.skip", request?.Skip);
        activity?.SetTag("journal.take", request?.Take);
        activity?.SetTag("journal.has_search", !string.IsNullOrWhiteSpace(request?.Search));

        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Skip < 0)
            {
                throw new SecureException("Skip must be greater than or equal to zero");
            }

            if (request.Take <= 0)
            {
                throw new SecureException("Take must be greater than zero");
            }

            var query = dbContext.ExceptionJournals
                .AsNoTracking()
                .AsQueryable();

            if (request.From.HasValue)
            {
                query = query.Where(journal => journal.CreatedAt >= request.From.Value);
            }

            if (request.To.HasValue)
            {
                query = query.Where(journal => journal.CreatedAt <= request.To.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(journal => journal.Text.Contains(request.Search));
            }

            var count = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(journal => journal.CreatedAt)
                .ThenByDescending(journal => journal.Id)
                .Skip(request.Skip)
                .Take(request.Take)
                .Select(journal => new GetJournalRangeItemResult
                {
                    Id = journal.Id,
                    EventId = journal.EventId,
                    CreatedAt = journal.CreatedAt
                })
                .ToListAsync(cancellationToken);

            activity?.SetTag("journal.total_count", count);
            activity?.SetTag("journal.items_count", items.Count);

            return new GetJournalRangeResult
            {
                Skip = request.Skip,
                Count = count,
                Items = items
            };
        }
        catch (Exception exception)
        {
            metrics.MarkFailure(exception);
            throw;
        }
    }
}
