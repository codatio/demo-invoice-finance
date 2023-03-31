namespace Codat.Demos.InvoiceFinancing.Api.Exceptions;

public class CodatDataClientException : Exception
{
    public CodatDataClientException(string message, Exception? innerException = default) : base(message, innerException) { }
}
