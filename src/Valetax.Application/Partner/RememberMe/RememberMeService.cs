using Microsoft.Extensions.Options;
using Valetax.Application.Abstractions;
using Valetax.Application.Telemetry;
using Valetax.Domain.Exceptions;

namespace Valetax.Application.Partner.RememberMe;

public sealed class RememberMeService(
    IOptions<AppAuthenticationOptions> authenticationOptions,
    IJwtTokenGenerator jwtTokenGenerator) : IRememberMeService
{
    public Task<RememberMeResult> ExecuteAsync(RememberMeRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationTracing.ActivitySource.StartActivity("partner.remember-me");
        using var metrics = ApplicationMetrics.StartUseCase("partner.remember-me");
        activity?.SetTag("auth.code.length", request?.Code?.Length);

        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                throw new SecureException("Code is required");
            }

            if (!string.Equals(request.Code, authenticationOptions.Value.RememberMeCode, StringComparison.Ordinal))
            {
                throw new SecureException("Invalid code");
            }

            var token = jwtTokenGenerator.GenerateToken(request.Code);
            activity?.SetTag("auth.token_issued", true);

            return Task.FromResult(new RememberMeResult
            {
                Token = token
            });
        }
        catch (Exception exception)
        {
            metrics.MarkFailure(exception);
            throw;
        }
    }
}
