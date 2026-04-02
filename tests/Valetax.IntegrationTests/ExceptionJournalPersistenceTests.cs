using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valetax.Api.Contracts;
using Valetax.Api.Controllers.Internal;
using Valetax.Infrastructure.Persistence;

namespace Valetax.IntegrationTests;

public sealed class ExceptionJournalPersistenceTests : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly PostgresWebApplicationFactory _factory;

    public ExceptionJournalPersistenceTests(PostgresWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClientAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task SecureException_IsPersistedToPostgresJournal()
    {
        var response = await _client.PostAsJsonAsync(
            "/_test/exceptions/secure",
            new TestExceptionRequest { Message = "You have to delete all children nodes first" });

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValetaxDbContext>();

        var journalEntry = await dbContext.ExceptionJournals
            .AsNoTracking()
            .SingleAsync(entry => entry.EventId == payload.Id);

        Assert.Equal(payload.Id, journalEntry.EventId);
        Assert.Equal("SecureException", journalEntry.ExceptionType);
        Assert.Equal("You have to delete all children nodes first", journalEntry.Message);
        Assert.Equal("/_test/exceptions/secure", journalEntry.Path);
        Assert.Equal("POST", journalEntry.Method);
        Assert.Contains("You have to delete all children nodes first", journalEntry.BodyParameters);
        Assert.DoesNotContain("\"StackTrace\"", journalEntry.Text);
        Assert.DoesNotContain("Microsoft.AspNetCore", journalEntry.Text);
    }

    [Fact]
    public async Task SecureException_MasksSensitiveQueryAndBodyValuesInJournal()
    {
        var response = await _client.PostAsJsonAsync(
            "/_test/exceptions/secure?code=top-secret-code",
            new
            {
                Message = "masked",
                Password = "super-secret-password",
                Token = "super-secret-token"
            });

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValetaxDbContext>();

        var journalEntry = await dbContext.ExceptionJournals
            .AsNoTracking()
            .SingleAsync(entry => entry.EventId == payload.Id);

        Assert.DoesNotContain("top-secret-code", journalEntry.QueryParameters);
        Assert.Contains("***", journalEntry.QueryParameters);

        Assert.DoesNotContain("super-secret-password", journalEntry.BodyParameters);
        Assert.DoesNotContain("super-secret-token", journalEntry.BodyParameters);
        Assert.Contains("***", journalEntry.BodyParameters);

        Assert.DoesNotContain("top-secret-code", journalEntry.Text);
        Assert.DoesNotContain("super-secret-password", journalEntry.Text);
        Assert.DoesNotContain("super-secret-token", journalEntry.Text);
    }
}
