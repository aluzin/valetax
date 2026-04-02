using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Valetax.Domain.Exceptions;

namespace Valetax.Api.Controllers.Internal;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("_test/exceptions")]
public class TestExceptionsController : ControllerBase
{
    [HttpPost("secure")]
    public IActionResult ThrowSecure([FromBody] TestExceptionRequest request, [FromServices] IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        throw new SecureException(request.Message);
    }

    [HttpPost("unhandled")]
    public IActionResult ThrowUnhandled([FromBody] TestExceptionRequest request, [FromServices] IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        throw new InvalidOperationException(request.Message);
    }
}
