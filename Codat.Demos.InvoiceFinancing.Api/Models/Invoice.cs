namespace Codat.Demos.InvoiceFinancing.Api.Models;

public record Invoice
{
    public string Id { get; init; }
    public string InvoiceNumber { get; init; }
    public DateTime IssueDate { get; init; }
    public DateTime DueDate { get; init; }
    public decimal AmountDue { get; init; }
    public Customer CustomerRef { get; init; }
}

public record InvoiceDecision
{
    public string InvoiceId { get; init; }
    public string InvoiceNo { get; init; }
    public decimal AmountDue { get; init; }
    public decimal OfferAmount { get; init; }
    public decimal Rate { get; init; }
}
