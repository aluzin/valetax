using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valetax.Api.Contracts;
using Valetax.Domain.Entities;
using Valetax.Infrastructure.Persistence;

namespace Valetax.IntegrationTests;

public sealed class TreeNodeDeleteTests : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly PostgresWebApplicationFactory _factory;

    public TreeNodeDeleteTests(PostgresWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClientAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Delete_WhenNodeExists_RemovesNodeAndAllDescendants()
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

        var grandChild = new Node
        {
            TreeId = tree.Id,
            ParentId = child.Id,
            Name = "GrandChild"
        };

        dbContext.Nodes.Add(grandChild);
        await dbContext.SaveChangesAsync();

        var sibling = new Node
        {
            TreeId = tree.Id,
            ParentId = root.Id,
            Name = "Sibling"
        };

        dbContext.Nodes.Add(sibling);
        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync(
            $"/api.user.tree.node.delete?nodeId={child.Id}",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var remainingNodes = await dbContext.Nodes
            .AsNoTracking()
            .Where(node => node.TreeId == tree.Id)
            .ToListAsync();

        Assert.DoesNotContain(remainingNodes, node => node.Id == child.Id);
        Assert.DoesNotContain(remainingNodes, node => node.Id == grandChild.Id);
        Assert.Contains(remainingNodes, node => node.Id == root.Id);
        Assert.Contains(remainingNodes, node => node.Id == sibling.Id);
    }

    [Fact]
    public async Task Delete_WhenNodeDoesNotExist_ReturnsSecureExceptionPayload()
    {
        var response = await _client.PostAsync(
            $"/api.user.tree.node.delete?nodeId={long.MaxValue}",
            content: null);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Secure", payload.Type);
        Assert.Equal("Node was not found", payload.Data.Message);
    }
}
