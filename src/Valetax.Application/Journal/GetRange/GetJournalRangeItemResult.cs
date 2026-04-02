namespace Valetax.Application.Journal.GetRange;

public sealed class GetJournalRangeItemResult
{
    public long Id { get; set; }

    public long EventId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
