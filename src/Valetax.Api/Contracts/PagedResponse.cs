namespace Valetax.Api.Contracts;

/// <summary>
/// Generic paged response.
/// </summary>
public class PagedResponse<T>
{
    /// <summary>
    /// Requested skip value.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Total number of items matching the query.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Current page items.
    /// </summary>
    public ICollection<T> Items { get; set; } = new List<T>();
}
