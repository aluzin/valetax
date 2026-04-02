using Valetax.Application.TreeNodes.CreateNode;
using Valetax.Domain.Entities;
using Valetax.Domain.Exceptions;
using Valetax.UnitTests.Common;

namespace Valetax.UnitTests.TreeNodes;

public sealed class CreateNodeServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTreeDoesNotExist_ThrowsSecureException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new CreateNodeService(dbContext, new NoOpDbUpdateExceptionMapper());

        var exception = await Assert.ThrowsAsync<SecureException>(() => service.ExecuteAsync(new CreateNodeRequest
        {
            TreeName = "missing",
            NodeName = "root"
        }));

        Assert.Equal("Tree was not found", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenParentIsMissing_CreatesChildUnderRootNode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Trees.Add(new Tree { Id = 1, Name = "main" });
        dbContext.Nodes.Add(new Node { Id = 10, TreeId = 1, Name = "main" });
        await dbContext.SaveChangesAsync();

        var service = new CreateNodeService(dbContext, new NoOpDbUpdateExceptionMapper());

        await service.ExecuteAsync(new CreateNodeRequest
        {
            TreeName = "main",
            NodeName = "root"
        });

        Assert.Equal(2, dbContext.Nodes.Count());
        var node = dbContext.Nodes.Single(node => node.Name == "root");
        Assert.Equal("root", node.Name);
        Assert.Equal(10, node.ParentId);
        Assert.Equal(1, node.TreeId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenParentIsMissingAndRootIsMissing_CreatesRootAndChild()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Trees.Add(new Tree { Id = 1, Name = "main" });
        await dbContext.SaveChangesAsync();

        var service = new CreateNodeService(dbContext, new NoOpDbUpdateExceptionMapper());

        await service.ExecuteAsync(new CreateNodeRequest
        {
            TreeName = "main",
            NodeName = "first-child"
        });

        Assert.Equal(2, dbContext.Nodes.Count());
        var rootNode = dbContext.Nodes.Single(node => node.ParentId == null);
        var childNode = dbContext.Nodes.Single(node => node.ParentId == rootNode.Id);

        Assert.Equal("main", rootNode.Name);
        Assert.Equal("first-child", childNode.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WhenParentDoesNotExist_ThrowsSecureException()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.Trees.Add(new Tree { Id = 1, Name = "main" });
        await dbContext.SaveChangesAsync();

        var service = new CreateNodeService(dbContext, new NoOpDbUpdateExceptionMapper());

        var exception = await Assert.ThrowsAsync<SecureException>(() => service.ExecuteAsync(new CreateNodeRequest
        {
            TreeName = "main",
            ParentNodeId = 999,
            NodeName = "child"
        }));

        Assert.Equal("Parent node was not found in the specified tree", exception.Message);
    }
}
