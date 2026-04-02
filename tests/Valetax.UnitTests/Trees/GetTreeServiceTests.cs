using Valetax.Application.Trees.GetTree;
using Valetax.Domain.Entities;
using Valetax.Domain.Exceptions;
using Valetax.UnitTests.Common;

namespace Valetax.UnitTests.Trees;

public sealed class GetTreeServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTreeDoesNotExist_CreatesTreeAndRootNode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new GetTreeService(dbContext);

        var result = await service.ExecuteAsync(new GetTreeRequest
        {
            TreeName = "main"
        });

        Assert.Single(dbContext.Trees);
        Assert.Equal("main", dbContext.Trees.Single().Name);
        var rootNode = Assert.Single(dbContext.Nodes);
        Assert.Equal("main", rootNode.Name);
        Assert.Null(rootNode.ParentId);
        Assert.Equal(rootNode.Id, result.Id);
        Assert.Equal("main", result.Name);
        Assert.Empty(result.Children);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRootExists_ReturnsRecursiveTree()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var tree = new Tree { Id = 10, Name = "main" };
        var root = new Node { Id = 100, TreeId = 10, Name = "root" };
        var child = new Node { Id = 101, TreeId = 10, ParentId = 100, Name = "child" };
        var grandChild = new Node { Id = 102, TreeId = 10, ParentId = 101, Name = "grand-child" };

        dbContext.Trees.Add(tree);
        dbContext.Nodes.AddRange(root, child, grandChild);
        await dbContext.SaveChangesAsync();

        var service = new GetTreeService(dbContext);

        var result = await service.ExecuteAsync(new GetTreeRequest
        {
            TreeName = "main"
        });
        var childResult = Assert.Single(result.Children);
        var grandChildResult = Assert.Single(childResult.Children);

        Assert.Equal(100, result.Id);
        Assert.Equal("root", result.Name);
        Assert.Equal("child", childResult.Name);
        Assert.Equal("grand-child", grandChildResult.Name);
    }
}
