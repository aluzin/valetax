using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valetax.Api.Contracts;
using Valetax.Domain.Entities;
using Valetax.Infrastructure.Persistence;

namespace Valetax.IntegrationTests;

public sealed class TreeNodeRenameTests : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly PostgresWebApplicationFactory _factory;

    public TreeNodeRenameTests(PostgresWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClientAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Rename_WhenNodeExists_UpdatesNodeName()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValetaxDbContext>();

        var tree = new Tree
        {
            Name = $"tree-{Guid.NewGuid():N}"
        };

        dbContext.Trees.Add(tree);
        await dbContext.SaveChangesAsync();

        var root = new Node
        {
            TreeId = tree.Id,
            Name = "Root"
        };

        dbContext.Nodes.Add(root);
        await dbContext.SaveChangesAsync();

        var child = new Node
        {
            TreeId = tree.Id,
            ParentId = root.Id,
            Name = "Child"
        };

        dbContext.Nodes.Add(child);
        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync(
            $"/api.user.tree.node.rename?nodeId={child.Id}&newNodeName=RenamedChild",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var renamedNode = await dbContext.Nodes
            .AsNoTracking()
            .SingleAsync(node => node.Id == child.Id);

        Assert.Equal("RenamedChild", renamedNode.Name);
    }

    [Fact]
    public async Task Rename_WhenSiblingWithSameNameExists_ReturnsSecureExceptionPayload()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValetaxDbContext>();

        var tree = new Tree
        {
            Name = $"tree-{Guid.NewGuid():N}"
        };

        dbContext.Trees.Add(tree);
        await dbContext.SaveChangesAsync();

        var root = new Node
        {
            TreeId = tree.Id,
            Name = "Root"
        };

        dbContext.Nodes.Add(root);
        await dbContext.SaveChangesAsync();

        var child1 = new Node
        {
            TreeId = tree.Id,
            ParentId = root.Id,
            Name = "Child 1"
        };

        var child2 = new Node
        {
            TreeId = tree.Id,
            ParentId = root.Id,
            Name = "Child 2"
        };

        dbContext.Nodes.AddRange(child1, child2);
        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync(
            $"/api.user.tree.node.rename?nodeId={child2.Id}&newNodeName=Child 1",
            content: null);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Secure", payload.Type);
        Assert.Equal("Node with the same name already exists among siblings", payload.Data.Message);
    }

    [Fact]
    public async Task Rename_WhenNodeDoesNotExist_ReturnsSecureExceptionPayload()
    {
        var response = await _client.PostAsync(
            $"/api.user.tree.node.rename?nodeId={long.MaxValue}&newNodeName=RenamedChild",
            content: null);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Secure", payload.Type);
        Assert.Equal("Node was not found", payload.Data.Message);
    }
}
