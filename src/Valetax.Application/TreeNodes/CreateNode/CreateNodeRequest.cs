namespace Valetax.Application.TreeNodes.CreateNode;

public sealed class CreateNodeRequest
{
    public string TreeName { get; set; } = null!;

    public long? ParentNodeId { get; set; }

    public string NodeName { get; set; } = null!;
}
