using Codat.Demos.InvoiceFinancing.Api.DataClients;
using Codat.Demos.InvoiceFinancing.Api.Models;

namespace Codat.Demos.InvoiceFinancing.Api.Services;

public interface ICustomerRiskAssessor
{
    Task<CustomerRisk> AssessCustomerRisk(Guid companyId, Customer customer, IEnumerable<Invoice> unpaidInvoicesForCompany, decimal totalAmountDueForCompany);
}

public class CustomerRiskAssessor : ICustomerRiskAssessor
{
    private readonly ICodatDataClient _codatDataClient;

    public CustomerRiskAssessor(ICodatDataClient codatDataClient)
    {
        _codatDataClient = codatDataClient;
    }

    public async Task<CustomerRisk> AssessCustomerRisk(
        Guid companyId,
        Customer customer,
        IEnumerable<Invoice> unpaidInvoicesForCompany,
        decimal totalAmountDueForCompany
    )
    {
        var customerPaidInvoices = await _codatDataClient.GetPaidInvoicesForCustomerAsync(companyId, customer.Id);
        if (customerPaidInvoices.Count < 2)
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
}
