namespace Codat.Demos.InvoiceFinancing.Api.Models;

public record Company
{
    public Guid Id { get; init; }
    public string Name { get; init; }
}
