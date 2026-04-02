namespace Valetax.Application.Trees.GetTree;

public interface IGetTreeService
{
    Task<GetTreeNodeResult> ExecuteAsync(GetTreeRequest request, CancellationToken cancellationToken = default);
}
