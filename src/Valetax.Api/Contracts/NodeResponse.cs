namespace Valetax.Api.Contracts;

/// <summary>
/// Tree node response model.
/// </summary>
public class NodeResponse
{
    /// <summary>
    /// Node identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Node name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Child nodes.
    /// </summary>
    public ICollection<NodeResponse> Children { get; set; } = new List<NodeResponse>();
}
