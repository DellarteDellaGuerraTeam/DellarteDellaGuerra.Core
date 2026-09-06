using DellarteDellaGuerra.Domain.Common.Logging.Port;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Discards every log line. The use cases under test log what they did; none of them read a
/// logger back, so the content tests have nothing to assert on it.
/// </summary>
internal sealed class SilentLoggerFactory : ILoggerFactory
{
    public ILogger CreateLogger<T>() => new SilentLogger();

    private sealed class SilentLogger : ILogger
    {
        public void Debug(string message, Exception? exception = null) { }
        public void Info(string message, Exception? exception = null) { }
        public void Warn(string message, Exception? exception = null) { }
        public void Error(string message, Exception? exception = null) { }
        public void Fatal(string message, Exception? exception = null) { }
    }
}
