using Microsoft.EntityFrameworkCore;
using Valetax.Application.Abstractions;
using Valetax.Application.Telemetry;
using Valetax.Domain.Entities;
using Valetax.Domain.Exceptions;

namespace Valetax.Application.TreeNodes.CreateNode;

public sealed class CreateNodeService(
    IValetaxDbContext dbContext,
    IDbUpdateExceptionMapper dbUpdateExceptionMapper) : ICreateNodeService
{
    public async Task ExecuteAsync(CreateNodeRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationTracing.ActivitySource.StartActivity("tree-nodes.create");
        using var metrics = ApplicationMetrics.StartUseCase("tree-nodes.create");
        activity?.SetTag("tree.name", request?.TreeName);
        activity?.SetTag("node.parent_id", request?.ParentNodeId);
        activity?.SetTag("node.name", request?.NodeName);

        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.TreeName))
            {
                throw new SecureException("Tree name is required");
            }

            if (string.IsNullOrWhiteSpace(request.NodeName))
            {
                throw new SecureException("Node name is required");
            }

            var tree = await dbContext.Trees
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Name == request.TreeName, cancellationToken);

            if (tree is null)
            {
                throw new SecureException("Tree was not found");
            }

            Node? parentNode = null;

            if (request.ParentNodeId.HasValue)
            {
                activity?.SetTag("node.create_under_root", false);
                parentNode = await dbContext.Nodes
                    .AsTracking()
                    .FirstOrDefaultAsync(
                        node => node.Id == request.ParentNodeId.Value && node.TreeId == tree.Id,
                        cancellationToken);

                if (parentNode is null)
                {
                    throw new SecureException("Parent node was not found in the specified tree");
                }

                var siblingNameExists = await dbContext.Nodes
                    .AsNoTracking()
                    .AnyAsync(
                        node => node.TreeId == tree.Id
                                && node.ParentId == parentNode.Id
                                && node.Name == request.NodeName,
                        cancellationToken);

                if (siblingNameExists)
                {
                    throw new SecureException("Node with the same name already exists among siblings");
                }
            }
            else
            {
                activity?.SetTag("node.create_under_root", true);
                parentNode = await dbContext.Nodes
                    .AsTracking()
                    .FirstOrDefaultAsync(node => node.TreeId == tree.Id && node.ParentId == null, cancellationToken);

                if (parentNode is null)
                {
                    parentNode = new Node
                    {
                        Tree = tree,
                        Name = tree.Name
                    };

                    dbContext.Nodes.Add(parentNode);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    activity?.SetTag("tree.root.created", true);
                }
                else
                {
                    activity?.SetTag("tree.root.created", false);
                }

                var siblingNameExists = await dbContext.Nodes
                    .AsNoTracking()
                    .AnyAsync(
                        node => node.TreeId == tree.Id
                                && node.ParentId == parentNode.Id
                                && node.Name == request.NodeName,
                        cancellationToken);

                if (siblingNameExists)
                {
                    throw new SecureException("Node with the same name already exists among siblings");
                }
            }

            var nodeToCreate = new Node
            {
                Tree = tree,
                Parent = parentNode,
                ParentId = parentNode!.Id,
                Name = request.NodeName
            };

            dbContext.Nodes.Add(nodeToCreate);
            await dbContext.SaveChangesAsync(cancellationToken);
            activity?.SetTag("node.tree_id", tree.Id);
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
