using Microsoft.EntityFrameworkCore;
using Valetax.Application.Abstractions;
using Valetax.Application.Telemetry;
using Valetax.Domain.Exceptions;

namespace Valetax.Application.TreeNodes.DeleteNode;

public sealed class DeleteNodeService(IValetaxDbContext dbContext) : IDeleteNodeService
{
    public async Task ExecuteAsync(DeleteNodeRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationTracing.ActivitySource.StartActivity("tree-nodes.delete");
        using var metrics = ApplicationMetrics.StartUseCase("tree-nodes.delete");
        activity?.SetTag("node.id", request?.NodeId);

        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var node = await dbContext.Nodes
                .AsTracking()
                .FirstOrDefaultAsync(existingNode => existingNode.Id == request.NodeId, cancellationToken);

            if (node is null)
            {
                throw new SecureException("Node was not found");
            }

            var treeNodes = await dbContext.Nodes
                .Where(existingNode => existingNode.TreeId == node.TreeId)
                .ToListAsync(cancellationToken);

            var nodesLookup = treeNodes.ToLookup(existingNode => existingNode.ParentId);
            var subtreeNodeIds = new HashSet<long>();

            CollectSubtreeNodeIds(node.Id, nodesLookup, subtreeNodeIds);

            var nodesToDelete = treeNodes
                .Where(existingNode => subtreeNodeIds.Contains(existingNode.Id))
                .ToList();

            dbContext.Nodes.RemoveRange(nodesToDelete);
            await dbContext.SaveChangesAsync(cancellationToken);
            activity?.SetTag("node.tree_id", node.TreeId);
            activity?.SetTag("node.deleted_count", nodesToDelete.Count);
        }
        catch (Exception exception)
        {
            metrics.MarkFailure(exception);
            throw;
        }
    }

    private static void CollectSubtreeNodeIds(
        long nodeId,
        ILookup<long?, Domain.Entities.Node> nodesLookup,
        ISet<long> subtreeNodeIds)
    {
        if (!subtreeNodeIds.Add(nodeId))
        {
            return;
        }

        foreach (var childNode in nodesLookup[nodeId])
        {
            CollectSubtreeNodeIds(childNode.Id, nodesLookup, subtreeNodeIds);
        }
    }
}
