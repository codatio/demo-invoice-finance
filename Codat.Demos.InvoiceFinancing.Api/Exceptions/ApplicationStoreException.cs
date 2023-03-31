namespace Codat.Demos.InvoiceFinancing.Api.Exceptions;

public class ApplicationStoreException : Exception
{
    public ApplicationStoreException(string message, Exception? innerException = default) : base(message, innerException) { }
}
