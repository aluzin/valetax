using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valetax.Application.Partner.RememberMe;
using Valetax.Api.Contracts;

namespace Valetax.Api.Controllers;

[ApiController]
[Tags("user.partner")]
public class PartnerController(IRememberMeService rememberMeService) : ControllerBase
{
    /// <summary>
    /// Saves user by unique code and returns auth token, if authentication is enabled.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("/api.user.partner.rememberMe")]
    [ProducesResponseType(typeof(TokenInfoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenInfoResponse>> RememberMe([FromQuery] string code, CancellationToken cancellationToken)
    {
        var result = await rememberMeService.ExecuteAsync(new RememberMeRequest
        {
            Code = code
        }, cancellationToken);

        return Ok(new TokenInfoResponse
        {
            Token = result.Token
        });
    }
}
