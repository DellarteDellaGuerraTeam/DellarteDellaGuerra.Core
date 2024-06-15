
using System;
using DellarteDellaGuerra.Domain.Common.Logging.Port;

namespace DellarteDellaGuerra.Infrastructure.Logging
{
    public class NLogLoggerAdapter : ILogger
    {
        private readonly NLog.ILogger _logger;

        public NLogLoggerAdapter(NLog.ILogger logger)
        {
            _logger = logger;
        }

        public void Debug(string message, Exception? exception = null)
        {
            if (exception is null)
                _logger.Debug(message);
            else
                _logger.Debug(exception, message);
        }

        public void Info(string message, Exception? exception = null)
        {
            if (exception is null)
                _logger.Info(message);
            else
                _logger.Info(exception, message);
        }

        public void Warn(string message, Exception? exception = null)
        {
            if (exception is null)
                _logger.Warn(message);
            else
                _logger.Warn(exception, message);
        }

        public void Error(string message, Exception? exception = null)
        {
            if (exception is null)
                _logger.Error(message);
            else
                _logger.Error(exception, message);
        }

        public void Fatal(string message, Exception? exception = null)
        {
            if (exception is null)
                _logger.Fatal(message);
            else
                _logger.Fatal(exception, message);
        }
    }
}
