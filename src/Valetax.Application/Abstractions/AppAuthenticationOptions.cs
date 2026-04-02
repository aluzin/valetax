namespace Valetax.Application.Abstractions;

public sealed class AppAuthenticationOptions
{
    public string RememberMeCode { get; set; } = null!;

    public JwtTokenOptions Jwt { get; set; } = new();
}

public sealed class JwtTokenOptions
{
    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    public string SigningKey { get; set; } = null!;
}
