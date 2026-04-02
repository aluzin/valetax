using Microsoft.EntityFrameworkCore;
using Valetax.Application.Abstractions;
using Valetax.Domain.Exceptions;

namespace Valetax.UnitTests.Common;

internal sealed class NoOpDbUpdateExceptionMapper : IDbUpdateExceptionMapper
{
    public SecureException? TryMap(DbUpdateException exception) => null;
}
