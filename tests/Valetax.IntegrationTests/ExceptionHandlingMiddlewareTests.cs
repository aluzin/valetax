using System.Net;
using System.Net.Http.Json;
using Valetax.Api.Contracts;
using Valetax.Api.Controllers.Internal;

namespace Valetax.IntegrationTests;

public class ExceptionHandlingMiddlewareTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ExceptionHandlingMiddlewareTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SecureException_ReturnsExpectedPayload()
    {
        var response = await _client.PostAsJsonAsync(
            "/_test/exceptions/secure",
            new TestExceptionRequest { Message = "You have to delete all children nodes first" });

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Secure", payload.Type);
        Assert.True(payload.Id > 0);
        Assert.Equal("You have to delete all children nodes first", payload.Data.Message);
    }

    [Fact]
    public async Task UnhandledException_ReturnsGenericPayload()
    {
        var response = await _client.PostAsJsonAsync(
            "/_test/exceptions/unhandled",
            new TestExceptionRequest { Message = "boom" });

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(nameof(Exception), payload.Type);
        Assert.True(payload.Id > 0);
        Assert.Equal($"Internal server error ID = {payload.Id}", payload.Data.Message);
    }
}
