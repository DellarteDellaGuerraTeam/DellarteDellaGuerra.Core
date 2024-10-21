using System;
using DellarteDellaGuerra.Domain.Common.Logging.Port;

namespace DellarteDellaGuerra.Infrastructure.ExpandedTemplateApi.Logging;

public class ExpandedTemplateLogger(ILogger logger) : Bannerlord.ExpandedTemplate.API.Logging.ILogger
{
    public void Debug(string message)
    {
        logger.Debug(message);
    }

    public void Info(string message)
    {
        logger.Info(message);
    }

    public void Warn(string message)
    {
        logger.Warn(message);
    }

    public void Error(string message, Exception? exception = null)
    {
        logger.Error(message, exception);
    }

    public void Fatal(string message, Exception? exception = null)
    {
        logger.Fatal(message, exception);
    }
}