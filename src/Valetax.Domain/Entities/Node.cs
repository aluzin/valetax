namespace Valetax.Domain.Entities;

public class Node
{
    public long Id { get; set; }

    public long TreeId { get; set; }

    public long? ParentId { get; set; }

    public string Name { get; set; } = null!;

    public Tree Tree { get; set; } = null!;

    public Node? Parent { get; set; }

    public ICollection<Node> Children { get; set; } = new List<Node>();
}
