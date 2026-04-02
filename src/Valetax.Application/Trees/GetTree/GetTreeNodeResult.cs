namespace Valetax.Application.Trees.GetTree;

public sealed class GetTreeNodeResult
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public ICollection<GetTreeNodeResult> Children { get; set; } = new List<GetTreeNodeResult>();
}
