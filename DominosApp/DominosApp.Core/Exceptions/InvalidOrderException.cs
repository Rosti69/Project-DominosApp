namespace DominosApp.Core.Exceptions
{
    public class InvalidOrderException : Exception
    {
        public InvalidOrderException(string message) : base(message) { }
    }
}