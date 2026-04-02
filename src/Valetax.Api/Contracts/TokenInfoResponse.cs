namespace Valetax.Api.Contracts;

/// <summary>
/// Authentication token payload.
/// </summary>
public class TokenInfoResponse
{
    /// <summary>
    /// JWT or other bearer token.
    /// </summary>
    public string Token { get; set; } = null!;
}
