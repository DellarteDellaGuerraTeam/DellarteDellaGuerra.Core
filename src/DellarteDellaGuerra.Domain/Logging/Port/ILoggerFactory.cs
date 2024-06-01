namespace DellarteDellaGuerra.Domain.Logging.Port
{
    public interface ILoggerFactory
    {
        ILogger CreateLogger<T>();
    }
}
