using Microsoft.EntityFrameworkCore;
using Valetax.Application.Abstractions;
using Valetax.Application.Telemetry;
using Valetax.Domain.Entities;
using Valetax.Domain.Exceptions;

namespace Valetax.Application.Trees.GetTree;

public sealed class GetTreeService(IValetaxDbContext dbContext) : IGetTreeService
{
    public async Task<GetTreeNodeResult> ExecuteAsync(GetTreeRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationTracing.ActivitySource.StartActivity("trees.get");
        using var metrics = ApplicationMetrics.StartUseCase("trees.get");
        activity?.SetTag("tree.name", request?.TreeName);

        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.TreeName))
            {
                throw new SecureException("Tree name is required");
            }

            var tree = await dbContext.Trees
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Name == request.TreeName, cancellationToken);

            if (tree is null)
            {
                tree = new Tree { Name = request.TreeName };
                dbContext.Trees.Add(tree);
                await dbContext.SaveChangesAsync(cancellationToken);
                activity?.SetTag("tree.created", true);
            }
            else
            {
                activity?.SetTag("tree.created", false);
            }

            var nodes = await dbContext.Nodes
                .AsNoTracking()
                .Where(node => node.TreeId == tree.Id)
                .Select(node => new FlatNode
                {
                    Id = node.Id,
                    ParentId = node.ParentId,
                    Name = node.Name
                })
                .ToListAsync(cancellationToken);

            var rootNodes = nodes.Where(node => node.ParentId is null).ToList();

            if (rootNodes.Count == 0)
            {
                activity?.SetTag("tree.root.exists", false);

                var rootNode = new Node
                {
                    TreeId = tree.Id,
                    Name = tree.Name
                };

                dbContext.Nodes.Add(rootNode);
                await dbContext.SaveChangesAsync(cancellationToken);

                rootNodes.Add(new FlatNode
                {
                    Id = rootNode.Id,
                    ParentId = rootNode.ParentId,
                    Name = rootNode.Name
                });
                nodes.Add(rootNodes[0]);
                activity?.SetTag("tree.root.created", true);
            }
            else
            {
                activity?.SetTag("tree.root.created", false);
            }

            if (rootNodes.Count > 1)
            {
                activity?.SetTag("tree.root.count", rootNodes.Count);
                throw new InvalidOperationException("Tree must contain exactly one root node");
            }

            var nodesLookup = nodes.ToLookup(node => node.ParentId);
            activity?.SetTag("tree.node.count", nodes.Count);

            return BuildTree(rootNodes[0], nodesLookup);
        }
        catch (Exception exception)
        {
            metrics.MarkFailure(exception);
            throw;
        }
    }

    private static GetTreeNodeResult BuildTree(FlatNode node, ILookup<long?, FlatNode> nodesLookup)
    {
        var result = new GetTreeNodeResult
        {
            Id = node.Id,
            Name = node.Name
        };

        foreach (var child in nodesLookup[node.Id])
        {
            result.Children.Add(BuildTree(child, nodesLookup));
        }

        return result;
    }

    private sealed class FlatNode
    {
        public long Id { get; init; }

        public long? ParentId { get; init; }

        public string Name { get; init; } = null!;
    }
}
