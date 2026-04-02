using System.Diagnostics;
using System.Diagnostics.Metrics;
using Valetax.Domain.Exceptions;

namespace Valetax.Application.Telemetry;

public static class ApplicationMetrics
{
    public const string MeterName = "Valetax.Application";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> UseCaseExecutions = Meter.CreateCounter<long>(
        "valetax_use_case_executions",
        unit: "{execution}",
        description: "Number of application use case executions.");
    private static readonly Histogram<double> UseCaseDuration = Meter.CreateHistogram<double>(
        "valetax_use_case_duration_ms",
        unit: "ms",
        description: "Duration of application use case executions.");

    public static UseCaseMetricsScope StartUseCase(string useCase) => new(useCase);

    public sealed class UseCaseMetricsScope : IDisposable
    {
        private readonly string _useCase;
        private readonly long _startTimestamp;
        private string _outcome = "success";

        public UseCaseMetricsScope(string useCase)
        {
            _useCase = useCase;
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public void MarkFailure(Exception exception)
        {
            _outcome = exception switch
            {
                OperationCanceledException => "cancelled",
                SecureException => "secure_error",
                _ => "error"
            };
        }

        public void Dispose()
        {
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;
            var tags = new TagList
            {
                { "use_case", _useCase },
                { "outcome", _outcome }
            };

            UseCaseExecutions.Add(1, tags);
            UseCaseDuration.Record(elapsedMilliseconds, tags);
        }
    }
}
