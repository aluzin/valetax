namespace Valetax.Api.Contracts;

/// <summary>
/// Journal item metadata.
/// </summary>
public class JournalInfoResponse
{
    /// <summary>
    /// Internal journal record identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Unique event identifier returned to API clients.
    /// </summary>
    public long EventId { get; set; }

    /// <summary>
    /// Journal record creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
