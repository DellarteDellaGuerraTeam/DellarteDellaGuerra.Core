using Microsoft.Extensions.Logging;
using DomainILogger = DellarteDellaGuerra.Domain.Common.Logging.Port.ILogger;
using DomainILoggerFactory = DellarteDellaGuerra.Domain.Common.Logging.Port.ILoggerFactory;
using MsILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace DellarteDellaGuerra.Infrastructure.Logging;

public class LoggerFactory : DomainILoggerFactory
{
    private readonly MsILoggerFactory _msLoggerFactory;

    public LoggerFactory(MsILoggerFactory msLoggerFactory)
    {
        _msLoggerFactory = msLoggerFactory;
    }

    public DomainILogger CreateLogger<T>()
    {
        return new DomainMicrosoftLogger(_msLoggerFactory.CreateLogger<T>());
    }
}
