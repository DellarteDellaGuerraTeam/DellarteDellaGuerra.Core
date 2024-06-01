
using DellarteDellaGuerra.Domain.Logging.Port;

namespace DellarteDellaGuerra.Infrastructure.Logging
{
    public class NLogLoggerAdapter : ILogger
    {
        private readonly NLog.ILogger _logger;

        public NLogLoggerAdapter(NLog.ILogger logger)
        {
            _logger = logger;
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Fatal(string message)
        {
            _logger.Fatal(message);
        }
    }
}
