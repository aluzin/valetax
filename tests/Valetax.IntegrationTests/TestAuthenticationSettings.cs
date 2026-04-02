namespace Valetax.IntegrationTests;

internal static class TestAuthenticationSettings
{
    public const string RememberMeCode = "valetax-test-code";
    public const string JwtIssuer = "Valetax.Tests";
    public const string JwtAudience = "Valetax.Tests";
    public const string JwtSigningKey = "valetax-test-signing-key-32-characters";
}
