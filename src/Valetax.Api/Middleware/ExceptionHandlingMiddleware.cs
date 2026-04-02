using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using Valetax.Application.Abstractions;
using Valetax.Api.Contracts;
using Valetax.Domain.Entities;
using Valetax.Domain.Exceptions;

namespace Valetax.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private const string RedactedValue = "***";
    private static readonly HashSet<string> SensitiveFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "code",
        "token",
        "access_token",
        "refresh_token",
        "password",
        "secret",
        "signingKey",
        "signing_key",
        "authorization",
        "rememberMeCode",
        "jwt"
    };

    public async Task InvokeAsync(HttpContext context, IExceptionJournalWriter exceptionJournalWriter)
    {
        if (context.Request.Body.CanRead)
        {
            context.Request.EnableBuffering();
        }

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var eventId = GetUnixTimeMicroseconds();
            var body = await ReadBodyAsync(context.Request);
            var query = SerializeQuery(context.Request.Query);

            var journal = new ExceptionJournal
            {
                EventId = eventId,
                CreatedAt = DateTimeOffset.UtcNow,
                ExceptionType = exception.GetType().Name,
                Message = exception.Message,
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                QueryParameters = query,
                BodyParameters = body,
                StackTrace = exception.ToString(),
                Text = BuildJournalText(context, exception, eventId, query, body)
            };

            await PersistJournalAsync(exceptionJournalWriter, logger, journal, context.RequestAborted);

            logger.LogError(exception, "Unhandled exception. EventId: {EventId}", eventId);

            await WriteErrorResponseAsync(context, exception, eventId);
        }
    }

    private static async Task<string?> ReadBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0 || !request.Body.CanRead)
        {
            return null;
        }

        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0;

        return string.IsNullOrWhiteSpace(body) ? null : MaskBody(body);
    }

    private static string? SerializeQuery(IQueryCollection query)
    {
        if (query.Count == 0)
        {
            return null;
        }

        var values = query.ToDictionary(
            pair => pair.Key,
            pair => IsSensitiveField(pair.Key)
                ? pair.Value.Select(_ => RedactedValue).ToArray()
                : pair.Value.ToArray());

        return JsonSerializer.Serialize(values);
    }

    private static string BuildJournalText(
        HttpContext context,
        Exception exception,
        long eventId,
        string? query,
        string? body)
    {
        return JsonSerializer.Serialize(new
        {
            EventId = eventId,
            CreatedAt = DateTimeOffset.UtcNow,
            Path = context.Request.Path.Value,
            Method = context.Request.Method,
            Query = query,
            Body = body,
            Exception = exception.GetType().Name,
            exception.Message
        });
    }

    private static async Task PersistJournalAsync(
        IExceptionJournalWriter exceptionJournalWriter,
        ILogger logger,
        ExceptionJournal journal,
        CancellationToken cancellationToken)
    {
        try
        {
            await exceptionJournalWriter.WriteAsync(journal, cancellationToken);
        }
        catch (Exception journalException)
        {
            logger.LogError(
                journalException,
                "Failed to persist exception journal for EventId {EventId}",
                journal.EventId);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception exception, long eventId)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        var isSecureException = exception is SecureException;
        var response = new ErrorResponse
        {
            Type = isSecureException ? NormalizeExceptionType(exception.GetType().Name) : nameof(Exception),
            Id = eventId,
            Data = new ErrorDataResponse
            {
                Message = isSecureException
                    ? exception.Message
                    : $"Internal server error ID = {eventId}"
            }
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static string NormalizeExceptionType(string exceptionType)
    {
        const string suffix = "Exception";

        return exceptionType.EndsWith(suffix, StringComparison.Ordinal)
            ? exceptionType[..^suffix.Length]
            : exceptionType;
    }

    private static string MaskBody(string body)
    {
        try
        {
            var node = JsonNode.Parse(body);

            if (node is null)
            {
                return body;
            }

            MaskJsonNode(node);

            return node.ToJsonString();
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static void MaskJsonNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject.ToList())
                {
                    if (property.Key is not null && IsSensitiveField(property.Key))
                    {
                        jsonObject[property.Key] = RedactedValue;
                        continue;
                    }

                    if (property.Value is not null)
                    {
                        MaskJsonNode(property.Value);
                    }
                }

                break;

            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    if (item is not null)
                    {
                        MaskJsonNode(item);
                    }
                }

                break;
        }
    }

    private static bool IsSensitiveField(string fieldName) => SensitiveFieldNames.Contains(fieldName);

    private static long GetUnixTimeMicroseconds()
    {
        return (DateTimeOffset.UtcNow.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) / 10;
    }
}
