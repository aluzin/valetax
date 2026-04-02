using Microsoft.Extensions.Options;
using Valetax.Application.Abstractions;
using Valetax.Application.Partner.RememberMe;
using Valetax.Domain.Exceptions;
using Valetax.UnitTests.Common;

namespace Valetax.UnitTests.Partner;

public sealed class RememberMeServiceTests
{
    private readonly IOptions<AppAuthenticationOptions> _options = Options.Create(new AppAuthenticationOptions
    {
        RememberMeCode = "dev-code",
        Jwt = new JwtTokenOptions
        {
            Issuer = "Valetax",
            Audience = "Valetax",
            SigningKey = "unused-in-unit-tests"
        }
    });

    [Fact]
    public async Task ExecuteAsync_WhenCodeIsValid_ReturnsToken()
    {
        var service = new RememberMeService(_options, new FakeJwtTokenGenerator());

        var result = await service.ExecuteAsync(new RememberMeRequest
        {
            Code = "dev-code"
        });

        Assert.Equal("token-for-dev-code", result.Token);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCodeIsInvalid_ThrowsSecureException()
    {
        var service = new RememberMeService(_options, new FakeJwtTokenGenerator());

        var exception = await Assert.ThrowsAsync<SecureException>(() => service.ExecuteAsync(new RememberMeRequest
        {
            Code = "wrong-code"
        }));

        Assert.Equal("Invalid code", exception.Message);
    }
}
