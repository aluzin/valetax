using Microsoft.EntityFrameworkCore;
using Valetax.Application.Abstractions;
using Valetax.Application.Telemetry;
using Valetax.Domain.Exceptions;

namespace Valetax.Application.TreeNodes.RenameNode;

public sealed class RenameNodeService(
    IValetaxDbContext dbContext,
    IDbUpdateExceptionMapper dbUpdateExceptionMapper) : IRenameNodeService
{
    public async Task ExecuteAsync(RenameNodeRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationTracing.ActivitySource.StartActivity("tree-nodes.rename");
        using var metrics = ApplicationMetrics.StartUseCase("tree-nodes.rename");
        activity?.SetTag("node.id", request?.NodeId);
        activity?.SetTag("node.new_name", request?.NewNodeName);

        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.NewNodeName))
            {
                throw new SecureException("New node name is required");
            }

            var node = await dbContext.Nodes
                .AsTracking()
                .FirstOrDefaultAsync(existingNode => existingNode.Id == request.NodeId, cancellationToken);

            if (node is null)
            {
                throw new SecureException("Node was not found");
            }

            var siblingNameExists = await dbContext.Nodes
                .AsNoTracking()
                .AnyAsync(
                    existingNode => existingNode.TreeId == node.TreeId
                                    && existingNode.ParentId == node.ParentId
                                    && existingNode.Id != node.Id
                                    && existingNode.Name == request.NewNodeName,
                    cancellationToken);

            if (siblingNameExists)
            {
                throw new SecureException("Node with the same name already exists among siblings");
            }

            node.Name = request.NewNodeName;
            await dbContext.SaveChangesAsync(cancellationToken);
            activity?.SetTag("node.tree_id", node.TreeId);
        }
        catch (DbUpdateException exception) when (dbUpdateExceptionMapper.TryMap(exception) is { } secureException)
        {
            metrics.MarkFailure(secureException);
            throw secureException;
        }
        catch (Exception exception)
        {
            metrics.MarkFailure(exception);
            throw;
        }
    }
}
