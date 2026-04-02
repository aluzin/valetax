namespace Valetax.Application.TreeNodes.RenameNode;

public sealed class RenameNodeRequest
{
    public long NodeId { get; set; }

    public string NewNodeName { get; set; } = null!;
}
