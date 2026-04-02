using System.Diagnostics;

namespace Valetax.Application.Telemetry;

public static class ApplicationTracing
{
    public const string ActivitySourceName = "Valetax.Application";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
