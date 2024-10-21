namespace DellarteDellaGuerra.Domain.Common.Logging.Port
{
    public interface ILoggerFactory
    {
        ILogger CreateLogger<T>();
    }
}
