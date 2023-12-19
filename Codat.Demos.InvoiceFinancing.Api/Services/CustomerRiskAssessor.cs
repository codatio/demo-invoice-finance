using Codat.Demos.InvoiceFinancing.Api.Mappers;
using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Lending;
using Codat.Lending.Models.Operations;

namespace Codat.Demos.InvoiceFinancing.Api.Services;

public interface ICustomerRiskAssessor
{
    Task<CustomerRisk> AssessCustomerRisk(Guid companyId, Customer customer, IEnumerable<Invoice> unpaidInvoicesForCompany, decimal totalAmountDueForCompany);
}

public class CustomerRiskAssessor : ICustomerRiskAssessor
{
    private readonly ICodatLending _codatLending;

    public CustomerRiskAssessor(ICodatLending codatLending)
    {
        _codatLending = codatLending;
    }

    public async Task<CustomerRisk> AssessCustomerRisk(
        Guid companyId,
        Customer customer,
        IEnumerable<Invoice> unpaidInvoicesForCompany,
        decimal totalAmountDueForCompany
    )
    {
        var customerPaidInvoices = await GetPaidInvoicesForCustomerAsync(companyId.ToString(), customer.Id);
        if (customerPaidInvoices.Length < 2)
        {
            // Discard customers with fewer than 2 paid invoices (max risk)
            return new CustomerRisk
            {
                CustomerId = customer.Id,
                Risk = 1m
            };
        }

        var customerUnpaidInvoices = unpaidInvoicesForCompany.Where(y => y.CustomerRef.Id == customer.Id).ToList();
        var totalAmountDueForCustomer = customerUnpaidInvoices.Sum(y => y.AmountDue);
        var risk = totalAmountDueForCustomer / totalAmountDueForCompany;

        return new CustomerRisk
        {
            CustomerId = customer.Id,
            Risk = risk
        };
    }
    
    private async Task<Invoice[]> GetPaidInvoicesForCustomerAsync(string companyId, string customerId)
    {
        var invoices = new List<Invoice>();
        var page = 1;
        ListAccountingInvoicesResponse pagedResult;
        do
        {
            pagedResult = await _codatLending.AccountsReceivable.Invoices.ListAsync(new()
            {
                CompanyId = companyId,
                Query = $"status=paid&&customerRef.id={customerId}",
                Page = page
            });

            if (!pagedResult.RawResponse.IsSuccessStatusCode)
            {
                continue;
            }

            invoices.AddRange(pagedResult.AccountingInvoices.Results.Select(InvoiceMapper.MapToDomainModel));
            page++;
        } while (pagedResult.AccountingInvoices.PageNumber * pagedResult.AccountingInvoices.PageSize < pagedResult.AccountingInvoices.TotalResults);

        return invoices.ToArray();
    }
}
