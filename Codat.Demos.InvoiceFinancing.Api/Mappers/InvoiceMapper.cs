using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Lending.Models.Shared;

namespace Codat.Demos.InvoiceFinancing.Api.Mappers;

public class InvoiceMapper
{
    public static Invoice MapToDomainModel(AccountingInvoice data) 
        => new()
        {
            Id = data.Id,
            InvoiceNumber = data.InvoiceNumber,
            IssueDate = DateTime.Parse(data.IssueDate),
            DueDate = DateTime.Parse(data.DueDate),
            AmountDue = data.AmountDue,
            CustomerRef = new ()
            {
                Id = data.Id
            }
        };
}