namespace Codat.Demos.InvoiceFinancing.Api.Exceptions;

public class ApplicationOrchestratorException : Exception
{
    public ApplicationOrchestratorException(string message, Exception? innerException = default) : base(message, innerException) { }
}
