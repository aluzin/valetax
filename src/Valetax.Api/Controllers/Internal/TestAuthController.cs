using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Valetax.Api.Controllers.Internal;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("_test/auth")]
public class TestAuthController : ControllerBase
{
    [Authorize]
    [HttpGet("protected")]
    public IActionResult Protected([FromServices] IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok();
    }
}
