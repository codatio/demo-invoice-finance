using Codat.Demos.InvoiceFinancing.Api.Mappers;
using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Demos.InvoiceFinancing.Api.Services;
using Codat.Lending;
using Codat.Lending.Models.Operations;
using Codat.Lending.Models.Shared;
using Microsoft.Extensions.Options;

namespace Codat.Demos.InvoiceFinancing.Api.Orchestrators;

public interface IFinancingProcessor
{
    Task ProcessFinancingForApplication(Guid id);
}

public class FinancingProcessor : IFinancingProcessor
{
    private readonly IApplicationStore _applicationStore;
    private readonly ICodatLending _codatLending;
    private readonly ICustomerRiskAssessor _customerRiskAssessor;
    private readonly IInvoiceFinanceAssessor _invoiceFinanceAssessor;
    private readonly InvoiceFinancingParameters _parameters;

    public FinancingProcessor(
        IApplicationStore applicationStore,
        ICodatLending codatLending,
        ICustomerRiskAssessor customerRiskAssessor,
        IInvoiceFinanceAssessor invoiceFinanceAssessor,
        IOptions<InvoiceFinancingParameters> options
    )
    {
        _applicationStore = applicationStore;
        _codatLending = codatLending;
        _customerRiskAssessor = customerRiskAssessor;
        _invoiceFinanceAssessor = invoiceFinanceAssessor;
        _parameters = options.Value;
    }

    public async Task ProcessFinancingForApplication(Guid id)
    {
        _applicationStore.UpdateApplicationStatus(id, ApplicationStatus.Processing);
        var application = _applicationStore.GetApplication(id);

        try
        {
            var invoiceDecisions = await ProcessInvoicesAsync(application.CodatCompanyId);
            _applicationStore.AddInvoiceDecisions(id, invoiceDecisions);
            _applicationStore.UpdateApplicationStatus(id, ApplicationStatus.Complete);
        }
        catch (Exception)
        {
            _applicationStore.UpdateApplicationStatus(id, ApplicationStatus.ProcessingError);
            throw;
        }
    }

    private async Task<Invoice[]> GetUnpaidInvoicesAsync(string companyId)
    {
        var invoices = new List<Invoice>();
        var page = 1;
        ListAccountingInvoicesResponse pagedResult;
        do
        {
            pagedResult = await _codatLending.AccountsReceivable.Invoices.ListAsync(new()
            {
                CompanyId = companyId,
                Query = "{status=submitted||status=partiallyPaid}&&currency=USD&&{amountDue>50&&amountDue<=1000}",
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

    private async Task<List<InvoiceDecision>> ProcessInvoicesAsync(Guid companyId)
    {
        // Get all unpaid invoices for the company
        var unpaidInvoices = await GetUnpaidInvoicesAsync(companyId.ToString());
        var totalAmountDueForCompany = unpaidInvoices.Sum(x => x.AmountDue);

        // Get customers by id from the unpaid invoices
        var customerIds = unpaidInvoices.Select(x => x.CustomerRef.Id).Distinct();
        var customers = await GetCustomersAsync(companyId, customerIds);

        // Assess risk for each customer, discard those above concentration threshold
        var customerRiskTasks = customers.Select(x => _customerRiskAssessor.AssessCustomerRisk(companyId, x, unpaidInvoices, totalAmountDueForCompany));
        var customerRisks = await Task.WhenAll(customerRiskTasks);
        var lowRiskCustomerIds = customerRisks.Where(x => x.Risk < _parameters.RiskConcentrationThreshold).Select(x => x.CustomerId);

        // Calculate decisions per unpaid invoice for customers with low risk
        var invoiceDecisions = unpaidInvoices.Where(x => lowRiskCustomerIds.Contains(x.CustomerRef.Id))
            .Where(
                x =>
                {
                    // Discard invoices with fewer than 14 days left to pay
                    var daysLeftToPay = x.DueDate - DateTime.Today;
                    return daysLeftToPay >= TimeSpan.FromDays(14);
                }
            )
            .Select(_invoiceFinanceAssessor.AssessInvoice)
            .ToList();

        return invoiceDecisions;
    }

    private async Task<List<Customer>> GetCustomersAsync(Guid companyId, IEnumerable<string> customerIds)
    {
        // Get all customers then filter in memory
        // Codat query strings support up to 2048 characters, we may go over that by combining all customer IDs into a single query
        var customers = new List<Customer>();
        var page = 1;
        ListAccountingCustomersResponse pagedResult;
        do
        {
            pagedResult = await _codatLending.AccountsReceivable.Customers.ListAsync(new()
            {
                Page = page
            });
            
            
            if (!pagedResult.RawResponse.IsSuccessStatusCode)
            {
                continue;
            }

            customers.AddRange(pagedResult.AccountingCustomers.Results.Select(CustomerMapper.MapToDomainModel));
            page++;
            
            
        } while (pagedResult.AccountingCustomers.PageNumber * pagedResult.AccountingCustomers.PageSize < pagedResult.AccountingCustomers.TotalResults);
        
        return customers.Where(x => customerIds.Contains(x.Id) && x.IsUnitedStatesCustomer() && !string.IsNullOrEmpty(x.RegistrationNumber)).ToList();
    }
}
