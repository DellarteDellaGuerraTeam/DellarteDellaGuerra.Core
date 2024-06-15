namespace DellarteDellaGuerra.Domain.Common.Exception
{
    public class TechnicalException : System.Exception
    {
        public TechnicalException()
        {
        }

        public TechnicalException(string message) : base(message)
        {
        }

        public TechnicalException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}