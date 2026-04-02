using Valetax.Application.TreeNodes.DeleteNode;
using Valetax.Application.TreeNodes.RenameNode;
using Valetax.Domain.Entities;
using Valetax.Domain.Exceptions;
using Valetax.UnitTests.Common;

namespace Valetax.UnitTests.TreeNodes;

public sealed class RenameAndDeleteNodeServiceTests
{
    [Fact]
    public async Task RenameExecuteAsync_WhenSiblingNameExists_ThrowsSecureException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Nodes.AddRange(
            new Node { Id = 1, TreeId = 10, Name = "root" },
            new Node { Id = 2, TreeId = 10, ParentId = 1, Name = "child-1" },
            new Node { Id = 3, TreeId = 10, ParentId = 1, Name = "child-2" });
        await dbContext.SaveChangesAsync();

        var service = new RenameNodeService(dbContext, new NoOpDbUpdateExceptionMapper());

        var exception = await Assert.ThrowsAsync<SecureException>(() => service.ExecuteAsync(new RenameNodeRequest
        {
            NodeId = 2,
            NewNodeName = "child-2"
        }));

        Assert.Equal("Node with the same name already exists among siblings", exception.Message);
    }

    [Fact]
    public async Task RenameExecuteAsync_WhenNodeExists_RenamesNode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Nodes.Add(new Node { Id = 2, TreeId = 10, Name = "before" });
        await dbContext.SaveChangesAsync();

        var service = new RenameNodeService(dbContext, new NoOpDbUpdateExceptionMapper());

        await service.ExecuteAsync(new RenameNodeRequest
        {
            NodeId = 2,
            NewNodeName = "after"
        });

        Assert.Equal("after", dbContext.Nodes.Single().Name);
    }

    [Fact]
    public async Task DeleteExecuteAsync_RemovesNodeSubtreeOnly()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Nodes.AddRange(
            new Node { Id = 1, TreeId = 10, Name = "root" },
            new Node { Id = 2, TreeId = 10, ParentId = 1, Name = "left" },
            new Node { Id = 3, TreeId = 10, ParentId = 2, Name = "left-child" },
            new Node { Id = 4, TreeId = 10, ParentId = 1, Name = "right" });
        await dbContext.SaveChangesAsync();

        var service = new DeleteNodeService(dbContext);

        await service.ExecuteAsync(new DeleteNodeRequest
        {
            NodeId = 2
        });

        var remainingNodes = dbContext.Nodes.OrderBy(node => node.Id).ToList();
        Assert.Collection(
            remainingNodes,
            node => Assert.Equal(1, node.Id),
            node => Assert.Equal(4, node.Id));
    }
}
