using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Valetax.Api.Contracts;

namespace Valetax.IntegrationTests;

public sealed class PartnerRememberMeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PartnerRememberMeTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RememberMe_WithValidCode_ReturnsJwtToken()
    {
        var response = await _client.PostAsync(
            $"/api.user.partner.rememberMe?code={TestAuthenticationSettings.RememberMeCode}",
            content: null);

        var payload = await response.Content.ReadFromJsonAsync<TokenInfoResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Token));
    }

    [Fact]
    public async Task RememberMe_WithValidCode_ReturnsTokenAcceptedByProtectedEndpoint()
    {
        var tokenResponse = await _client.PostAsync(
            $"/api.user.partner.rememberMe?code={TestAuthenticationSettings.RememberMeCode}",
            content: null);

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<TokenInfoResponse>();

        Assert.NotNull(tokenPayload);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/_test/auth/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenPayload.Token);

        var protectedResponse = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task RememberMe_WithInvalidCode_ReturnsSecureExceptionPayload()
    {
        var response = await _client.PostAsync(
            "/api.user.partner.rememberMe?code=wrong-code",
            content: null);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Secure", payload.Type);
        Assert.Equal("Invalid code", payload.Data.Message);
    }
}
