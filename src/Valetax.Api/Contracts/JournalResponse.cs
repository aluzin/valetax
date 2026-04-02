namespace Valetax.Api.Contracts;

/// <summary>
/// Full journal record payload.
/// </summary>
public class JournalResponse : JournalInfoResponse
{
    /// <summary>
    /// Serialized journal text with exception details.
    /// </summary>
    public string Text { get; set; } = null!;
}
