using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using Valetax.Api.Contracts;

namespace Valetax.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:RememberMeCode"] = TestAuthenticationSettings.RememberMeCode,
                ["Authentication:Jwt:Issuer"] = TestAuthenticationSettings.JwtIssuer,
                ["Authentication:Jwt:Audience"] = TestAuthenticationSettings.JwtAudience,
                ["Authentication:Jwt:SigningKey"] = TestAuthenticationSettings.JwtSigningKey
            });
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var tokenResponse = await client.PostAsync(
            $"/api.user.partner.rememberMe?code={TestAuthenticationSettings.RememberMeCode}",
            content: null);
        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<TokenInfoResponse>();

        if (tokenPayload is null || string.IsNullOrWhiteSpace(tokenPayload.Token))
        {
            throw new InvalidOperationException("Failed to obtain JWT token for integration tests.");
        }

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenPayload.Token);

        return client;
    }
}
