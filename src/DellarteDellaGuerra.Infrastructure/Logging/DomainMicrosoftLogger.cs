using System;
using Microsoft.Extensions.Logging;
using ILogger = DellarteDellaGuerra.Domain.Common.Logging.Port.ILogger;

namespace DellarteDellaGuerra.Infrastructure.Logging;

internal class DomainMicrosoftLogger : ILogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    public DomainMicrosoftLogger(Microsoft.Extensions.Logging.ILogger logger)
    {
        _logger = logger;
    }

    public void Debug(string message, Exception? exception = null)
    {
        _logger.Log(LogLevel.Debug, exception, message);
    }

    public void Info(string message, Exception? exception = null)
    {
        _logger.Log(LogLevel.Information, exception, message);
    }

    public void Warn(string message, Exception? exception = null)
    {
        _logger.Log(LogLevel.Warning, exception, message);
    }

    public void Error(string message, Exception? exception = null)
    {
        _logger.Log(LogLevel.Error, exception, message);
    }

    public void Fatal(string message, Exception? exception = null)
    {
        _logger.Log(LogLevel.Critical, exception, message);
    }
}