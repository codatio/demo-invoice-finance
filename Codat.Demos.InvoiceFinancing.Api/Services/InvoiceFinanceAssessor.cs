using Codat.Demos.InvoiceFinancing.Api.Models;

namespace Codat.Demos.InvoiceFinancing.Api.Services;

public interface IInvoiceFinanceAssessor
{
    InvoiceDecision AssessInvoice(Invoice invoice);
}

public class InvoiceFinanceAssessor : IInvoiceFinanceAssessor
{
    public InvoiceDecision AssessInvoice(Invoice invoice)
    {
        var terms = invoice.DueDate - invoice.IssueDate;
        var daysLeftToPay = invoice.DueDate - DateTime.Today;
        var ratio = daysLeftToPay / terms;
        var rate = decimal.Round((decimal) (5 - 4 * ratio), 1);

        return new InvoiceDecision
        {
            InvoiceId = invoice.Id,
            InvoiceNo = invoice.InvoiceNumber,
            AmountDue = invoice.AmountDue,
            OfferAmount = invoice.AmountDue * 0.9m,
            Rate = rate
        };
    }
}
