using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valetax.Api.Contracts;
using Valetax.Domain.Entities;
using Valetax.Infrastructure.Persistence;

namespace Valetax.IntegrationTests;

public sealed class TreeNodeCreateTests : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly PostgresWebApplicationFactory _factory;

    public TreeNodeCreateTests(PostgresWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClientAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Create_WhenTreeDoesNotExist_ReturnsSecureExceptionPayload()
    {
        var response = await _client.PostAsync(
            $"/api.user.tree.node.create?treeName=missing-{Guid.NewGuid():N}&nodeName=Child",
            content: null);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Secure", payload.Type);
        Assert.Equal("Tree was not found", payload.Data.Message);
    }

    [Fact]
    public async Task Create_WithoutParent_CreatesFirstLevelChildUnderExistingRoot()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValetaxDbContext>();

        var tree = new Tree
        {
            Name = $"tree-{Guid.NewGuid():N}"
        };

        dbContext.Trees.Add(tree);
        dbContext.Nodes.Add(new Node
        {
            Tree = tree,
            Name = tree.Name
        });
        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync(
            $"/api.user.tree.node.create?treeName={tree.Name}&nodeName=FirstChild",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rootNode = await dbContext.Nodes.SingleAsync(node => node.TreeId == tree.Id && node.ParentId == null);
        var childNode = await dbContext.Nodes.SingleAsync(node => node.TreeId == tree.Id && node.ParentId == rootNode.Id);

        Assert.Equal(tree.Name, rootNode.Name);
        Assert.Equal("FirstChild", childNode.Name);
    }

    [Fact]
    public async Task Create_WithoutParent_WhenRootIsMissing_CreatesRootAndChild()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValetaxDbContext>();

        var tree = new Tree
        {
            Name = $"tree-{Guid.NewGuid():N}"
        };

        dbContext.Trees.Add(tree);
        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync(
            $"/api.user.tree.node.create?treeName={tree.Name}&nodeName=FirstChild",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rootNode = await dbContext.Nodes.SingleAsync(node => node.TreeId == tree.Id && node.ParentId == null);
        var childNode = await dbContext.Nodes.SingleAsync(node => node.TreeId == tree.Id && node.ParentId == rootNode.Id);

        Assert.Equal(tree.Name, rootNode.Name);
        Assert.Equal("FirstChild", childNode.Name);
    }

    [Fact]
    public async Task Create_WithoutParent_Twice_CreatesTwoFirstLevelChildrenUnderTheSameRoot()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValetaxDbContext>();

        var tree = new Tree
        {
            Name = $"tree-{Guid.NewGuid():N}"
        };

        dbContext.Trees.Add(tree);
        dbContext.Nodes.Add(new Node
        {
            Tree = tree,
            Name = tree.Name
        });
        await dbContext.SaveChangesAsync();

        var firstResponse = await _client.PostAsync(
            $"/api.user.tree.node.create?treeName={tree.Name}&nodeName=ChildOne",
            content: null);
        var secondResponse = await _client.PostAsync(
            $"/api.user.tree.node.create?treeName={tree.Name}&nodeName=ChildTwo",
            content: null);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var rootNode = await dbContext.Nodes.SingleAsync(node => node.TreeId == tree.Id && node.ParentId == null);
        var firstLevelChildren = await dbContext.Nodes
            .Where(node => node.TreeId == tree.Id && node.ParentId == rootNode.Id)
            .OrderBy(node => node.Name)
            .ToListAsync();

        Assert.Collection(
            firstLevelChildren,
            node => Assert.Equal("ChildOne", node.Name),
            node => Assert.Equal("ChildTwo", node.Name));
    }

    [Fact]
    public async Task Create_WithoutParent_WithDuplicateFirstLevelName_ReturnsSecureExceptionPayload()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValetaxDbContext>();

        var tree = new Tree
        {
            Name = $"tree-{Guid.NewGuid():N}"
        };

        dbContext.Trees.Add(tree);
        dbContext.Nodes.Add(new Node
        {
            Tree = tree,
            Name = tree.Name
        });
        await dbContext.SaveChangesAsync();

        var rootNode = await dbContext.Nodes.SingleAsync(node => node.TreeId == tree.Id && node.ParentId == null);
        dbContext.Nodes.Add(new Node
        {
            TreeId = tree.Id,
            ParentId = rootNode.Id,
            Name = "Child"
        });
        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync(
            $"/api.user.tree.node.create?treeName={tree.Name}&nodeName=Child",
            content: null);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Secure", payload.Type);
        Assert.Equal("Node with the same name already exists among siblings", payload.Data.Message);
    }

    [Fact]
    public async Task Create_WithParent_CreatesChildUnderSpecifiedNode()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValetaxDbContext>();

        var tree = new Tree
        {
            Name = $"tree-{Guid.NewGuid():N}"
        };

        dbContext.Trees.Add(tree);
        dbContext.Nodes.Add(new Node
        {
            Tree = tree,
            Name = tree.Name
        });
        await dbContext.SaveChangesAsync();

        var rootNode = await dbContext.Nodes.SingleAsync(node => node.TreeId == tree.Id && node.ParentId == null);
        var parentNode = new Node
        {
            TreeId = tree.Id,
            ParentId = rootNode.Id,
            Name = "Parent"
        };

        dbContext.Nodes.Add(parentNode);
        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync(
            $"/api.user.tree.node.create?treeName={tree.Name}&parentNodeId={parentNode.Id}&nodeName=Child",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var childNode = await dbContext.Nodes.SingleAsync(node => node.TreeId == tree.Id && node.ParentId == parentNode.Id);
        Assert.Equal("Child", childNode.Name);
    }
}
