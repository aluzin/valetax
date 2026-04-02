namespace Valetax.Domain.Entities;

public class ExceptionJournal
{
    public long Id { get; set; }

    public long EventId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string ExceptionType { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? Path { get; set; }

    public string? Method { get; set; }

    public string? QueryParameters { get; set; }

    public string? BodyParameters { get; set; }

    public string? StackTrace { get; set; }

    public string Text { get; set; } = null!;
}
