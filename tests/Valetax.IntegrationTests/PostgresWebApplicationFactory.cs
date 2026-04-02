using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Valetax.Api.Contracts;
using Valetax.Application.Abstractions;
using Valetax.Infrastructure.Persistence;
using Valetax.Infrastructure.Persistence.Services;

namespace Valetax.IntegrationTests;

public sealed class PostgresWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("valetax_tests")
        .WithUsername("valetax")
        .WithPassword("valetax")
        .Build();

    public Task InitializeAsync()
    {
        return _database.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = _database.GetConnectionString(),
                ["Database:MigrateOnStartup"] = bool.TrueString,
                ["OpenTelemetry:OtlpEndpoint"] = string.Empty,
                ["Authentication:RememberMeCode"] = TestAuthenticationSettings.RememberMeCode,
                ["Authentication:Jwt:Issuer"] = TestAuthenticationSettings.JwtIssuer,
                ["Authentication:Jwt:Audience"] = TestAuthenticationSettings.JwtAudience,
                ["Authentication:Jwt:SigningKey"] = TestAuthenticationSettings.JwtSigningKey
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ValetaxDbContext>();
            services.RemoveAll<DbContextOptions<ValetaxDbContext>>();
            services.RemoveAll<IValetaxDbContext>();
            services.RemoveAll<IExceptionJournalWriter>();

            services.PostConfigure<DatabaseOptions>(options =>
            {
                options.ConnectionString = _database.GetConnectionString();
                options.MigrateOnStartup = true;
            });

            services.AddDbContext<ValetaxDbContext>(options => options.UseNpgsql(_database.GetConnectionString()));
            services.AddScoped<IValetaxDbContext>(provider => provider.GetRequiredService<ValetaxDbContext>());
            services.AddScoped<IExceptionJournalWriter, ExceptionJournalWriter>();
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
