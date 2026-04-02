using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Valetax.Api.Contracts;
using Valetax.Domain.Entities;
using Valetax.Infrastructure.Persistence;

namespace Valetax.IntegrationTests;

public sealed class TreeGetTests : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly PostgresWebApplicationFactory _factory;

    public TreeGetTests(PostgresWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClientAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Get_ReturnsRootNodeWithChildren()
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

        dbContext.Nodes.AddRange(
            new Node
            {
                TreeId = tree.Id,
                ParentId = root.Id,
                Name = "Child 1"
            },
            new Node
            {
                TreeId = tree.Id,
                ParentId = root.Id,
                Name = "Child 2"
            });

        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync($"/api.user.tree.get?treeName={tree.Name}", content: null);
        var payload = await response.Content.ReadFromJsonAsync<NodeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(root.Id, payload.Id);
        Assert.Equal("Root", payload.Name);
        Assert.Equal(2, payload.Children.Count);
        Assert.Contains(payload.Children, child => child.Name == "Child 1");
        Assert.Contains(payload.Children, child => child.Name == "Child 2");
    }
}
