using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

// Define the source-generated logger methods
public static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Handled request {RequestName} for user {UserId} in {ElapsedMs}ms")]
    public static partial void HandleRequest(this ILogger logger, string requestName, int userId, double elapsedMs);
}


[MemoryDiagnoser] // This attribute is crucial to see memory allocations
public class LoggingBenchmark
{
    // Use a NullLogger to isolate the performance of the logging call itself,
    // removing the console/file I/O from the measurement.
    private readonly ILogger _logger = new NullLogger();
    private const string RequestName = "GetUser";
    private const int UserId = 123;
    private const double ElapsedMs = 45.67;

    [Benchmark(Baseline = true)]
    public void TraditionalLogger()
    {
        // This call causes boxing for UserId (int) and ElapsedMs (double)
        // It also allocates an object[] array for the parameters.
        _logger.LogInformation("Handled request {RequestName} for user {UserId} in {ElapsedMs}ms",
            RequestName, UserId, ElapsedMs);
    }

    [Benchmark]
    public void SourceGeneratedLogger()
    {
        // This is a direct, strongly-typed call with no boxing or array allocation.
        _logger.HandleRequest(RequestName, UserId, ElapsedMs);
    }
}


public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<LoggingBenchmark>();
    }
}

// Minimal ILogger implementation for benchmarking purposes.
// From: https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Logging.Abstractions/src/NullLogger.cs
internal sealed class NullLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    private sealed class NullScope : IDisposable { public static NullScope Instance { get; } = new NullScope(); public void Dispose() { } }
}