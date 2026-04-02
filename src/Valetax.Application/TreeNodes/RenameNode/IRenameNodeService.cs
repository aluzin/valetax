namespace Valetax.Application.TreeNodes.RenameNode;

public interface IRenameNodeService
{
    Task ExecuteAsync(RenameNodeRequest request, CancellationToken cancellationToken = default);
}
