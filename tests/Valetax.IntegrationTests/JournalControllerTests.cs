using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Valetax.Api.Contracts;
using Valetax.Domain.Entities;
using Valetax.Infrastructure.Persistence;

namespace Valetax.IntegrationTests;

public sealed class JournalControllerTests : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly PostgresWebApplicationFactory _factory;

    public JournalControllerTests(PostgresWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClientAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetSingle_ReturnsJournalByEventId()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValetaxDbContext>();

        var entry = new ExceptionJournal
        {
            EventId = GetUnixTimeMicroseconds(DateTimeOffset.UtcNow),
            CreatedAt = DateTimeOffset.UtcNow,
            ExceptionType = "SecureException",
            Message = "Test message",
            Text = "Full journal text"
        };

        dbContext.ExceptionJournals.Add(entry);
        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync($"/api.user.journal.getSingle?id={entry.EventId}", content: null);
        var payload = await response.Content.ReadFromJsonAsync<JournalResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(entry.Id, payload.Id);
        Assert.Equal(entry.EventId, payload.EventId);
        Assert.Equal("Full journal text", payload.Text);
    }

    [Fact]
    public async Task GetRange_ReturnsPagedFilteredJournalItems()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValetaxDbContext>();

        var now = DateTimeOffset.UtcNow;

        dbContext.ExceptionJournals.AddRange(
            new ExceptionJournal
            {
                EventId = GetUnixTimeMicroseconds(now) - 2,
                CreatedAt = now.AddMinutes(-10),
                ExceptionType = "SecureException",
                Message = "Alpha",
                Text = "alpha search text"
            },
            new ExceptionJournal
            {
                EventId = GetUnixTimeMicroseconds(now) - 1,
                CreatedAt = now.AddMinutes(-5),
                ExceptionType = "Exception",
                Message = "Beta",
                Text = "beta search text"
            },
            new ExceptionJournal
            {
                EventId = GetUnixTimeMicroseconds(now),
                CreatedAt = now,
                ExceptionType = "Exception",
                Message = "Gamma",
                Text = "gamma"
            });

        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync(
            "/api.user.journal.getRange?skip=0&take=10",
            new JournalFilterRequest
            {
                Search = "search",
                From = now.AddMinutes(-15),
                To = now.AddMinutes(-1)
            });

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<JournalInfoResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(0, payload.Skip);
        Assert.Equal(2, payload.Count);
        Assert.Equal(2, payload.Items.Count);
    }

    private static long GetUnixTimeMicroseconds(DateTimeOffset value)
    {
        return (value.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) / 10;
    }
}
