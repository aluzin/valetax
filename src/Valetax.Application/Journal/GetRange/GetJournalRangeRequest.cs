namespace Valetax.Application.Journal.GetRange;

public sealed class GetJournalRangeRequest
{
    public int Skip { get; set; }

    public int Take { get; set; }

    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    public string? Search { get; set; }
}
