using DellarteDellaGuerra.Domain.Common.Logging.Port;
using ILogger = Bannerlord.ExpandedTemplate.API.Logging.ILogger;

namespace DellarteDellaGuerra.Infrastructure.ExpandedTemplateApi.Logging;

public class ExpandedTemplateLoggerFactory
    (ILoggerFactory loggerFactory) : Bannerlord.ExpandedTemplate.API.Logging.ILoggerFactory
{
    public ILogger CreateLogger<T>()
    {
        return new ExpandedTemplateLogger(loggerFactory.CreateLogger<T>());
    }
}