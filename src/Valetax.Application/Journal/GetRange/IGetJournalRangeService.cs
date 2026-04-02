namespace Valetax.Application.Journal.GetRange;

public interface IGetJournalRangeService
{
    Task<GetJournalRangeResult> ExecuteAsync(GetJournalRangeRequest request, CancellationToken cancellationToken = default);
}
