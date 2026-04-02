namespace Valetax.Application.Partner.RememberMe;

public interface IRememberMeService
{
    Task<RememberMeResult> ExecuteAsync(RememberMeRequest request, CancellationToken cancellationToken = default);
}
