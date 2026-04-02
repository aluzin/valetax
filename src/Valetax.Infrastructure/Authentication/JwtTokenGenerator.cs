using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Valetax.Application.Abstractions;

namespace Valetax.Infrastructure.Authentication;

public sealed class JwtTokenGenerator(IOptions<AppAuthenticationOptions> authenticationOptions) : IJwtTokenGenerator
{
    public string GenerateToken(string subject)
    {
        var options = authenticationOptions.Value;
        var now = DateTime.UtcNow;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, subject)
            }),
            Expires = now.AddHours(1),
            Issuer = options.Jwt.Issuer,
            Audience = options.Jwt.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(securityToken);
    }
}
