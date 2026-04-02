using Microsoft.EntityFrameworkCore;
using Valetax.Domain.Exceptions;

namespace Valetax.Application.Abstractions;

public interface IDbUpdateExceptionMapper
{
    SecureException? TryMap(DbUpdateException exception);
}
