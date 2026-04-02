namespace Valetax.Api.Contracts;

/// <summary>
/// Optional filter for journal search.
/// </summary>
public class JournalFilterRequest
{
    /// <summary>
    /// Inclusive lower bound for event creation time.
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Inclusive upper bound for event creation time.
    /// </summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Free-text search query.
    /// </summary>
    public string? Search { get; set; }
}
