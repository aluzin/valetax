namespace Valetax.Application.Journal.GetRange;

public sealed class GetJournalRangeResult
{
    public int Skip { get; set; }

    public int Count { get; set; }

    public ICollection<GetJournalRangeItemResult> Items { get; set; } = new List<GetJournalRangeItemResult>();
}
