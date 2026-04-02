namespace Valetax.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(string subject);
}
