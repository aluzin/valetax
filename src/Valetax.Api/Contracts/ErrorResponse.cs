namespace Valetax.Api.Contracts;

public class ErrorResponse
{
    public string Type { get; set; } = null!;

    public long Id { get; set; }

    public ErrorDataResponse Data { get; set; } = null!;
}
