using Valetax.Application.Abstractions;

namespace Valetax.UnitTests.Common;

internal sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public string GenerateToken(string subject) => $"token-for-{subject}";
}
