namespace Valetax.Application.TreeNodes.DeleteNode;

public interface IDeleteNodeService
{
    Task ExecuteAsync(DeleteNodeRequest request, CancellationToken cancellationToken = default);
}
