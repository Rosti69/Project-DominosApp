namespace DominosApp.Core.Exceptions
{
    public class NotFoundException : Exception
    {
        private const string DefaultErrorMessage = "Not Found";

        public NotFoundException()
            : base(DefaultErrorMessage) { }

        public NotFoundException(string customMessage)
            : base(customMessage) { }
    }
}
