using Microsoft.EntityFrameworkCore;
using Npgsql;
using Valetax.Application.Abstractions;
using Valetax.Domain.Exceptions;

namespace Valetax.Infrastructure.Persistence.Services;

public sealed class PostgresDbUpdateExceptionMapper : IDbUpdateExceptionMapper
{
    private const string RootNodeConstraintName = "UX_nodes_tree_root";
    private const string SiblingNodeConstraintName = "UX_nodes_tree_parent_name";

    public SecureException? TryMap(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException ||
            postgresException.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return null;
        }

        return postgresException.ConstraintName switch
        {
            RootNodeConstraintName => new SecureException("Tree root node already exists"),
            SiblingNodeConstraintName => new SecureException("Node with the same name already exists among siblings"),
            _ => null
        };
    }
}
