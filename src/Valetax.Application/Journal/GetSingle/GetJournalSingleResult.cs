namespace Valetax.Application.Journal.GetSingle;

public sealed class GetJournalSingleResult
{
    public long Id { get; set; }

    public long EventId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string Text { get; set; } = null!;
}
