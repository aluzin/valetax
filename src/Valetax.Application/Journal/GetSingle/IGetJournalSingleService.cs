namespace Valetax.Application.Journal.GetSingle;

public interface IGetJournalSingleService
{
    Task<GetJournalSingleResult?> ExecuteAsync(GetJournalSingleRequest request, CancellationToken cancellationToken = default);
}
