namespace Valetax.Application.TreeNodes.CreateNode;

public interface ICreateNodeService
{
    Task ExecuteAsync(CreateNodeRequest request, CancellationToken cancellationToken = default);
}
