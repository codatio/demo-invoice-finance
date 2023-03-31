using Codat.Demos.InvoiceFinancing.Api.DataClients;
using Codat.Demos.InvoiceFinancing.Api.Models;

namespace Codat.Demos.InvoiceFinancing.Api.Tests.WAF;

public class MockCodatDataClient : ICodatDataClient
{
    private readonly Dictionary<Guid, List<Customer>> _customers = new();
    private readonly Dictionary<string, List<Invoice>> _paidInvoicesByCustomer = new();
    private readonly Dictionary<Guid, List<Invoice>> _unpaidInvoices = new();

    private Company _company = new()
    {
        Id = Guid.NewGuid(),
        Name = "company"
    };


    public Task<Company> CreateCompanyAsync(string companyName)
    {
        _company = new Company
        {
            Id = Guid.NewGuid(),
            Name = companyName
        };

        return Task.FromResult(_company);
    }

    public Task<List<Platform>> GetAccountingPlatformsAsync()
    {
        return Task.FromResult(new List<Platform> { new() { Key = "mqjo" } });
    }

    public Task<List<Invoice>> GetUnpaidInvoicesAsync(Guid companyId)
    {
        if (_unpaidInvoices.TryGetValue(companyId, out var invoices))
        {
            return Task.FromResult(invoices);
        }

        return Task.FromResult(new List<Invoice>());
    }

    public Task<List<Customer>> GetCustomersAsync(Guid companyId)
    {
        if (_customers.TryGetValue(companyId, out var customers))
        {
            return Task.FromResult(customers);
        }

        return Task.FromResult(new List<Customer>());
    }

    public Task<List<Invoice>> GetPaidInvoicesForCustomerAsync(Guid companyId, string customerId)
    {
        if (_paidInvoicesByCustomer.TryGetValue(customerId, out var invoices))
        {
            return Task.FromResult(invoices);
        }

        return Task.FromResult(new List<Invoice>());
    }

    public Company GetCreatedCompany()
    {
        return _company;
    }

    public void SetupUnpaidInvoices(Guid companyId, List<Invoice> unpaidInvoices)
    {
        _unpaidInvoices[companyId] = unpaidInvoices;
    }

    public void SetupCustomer(Guid companyId, Customer customer)
    {
        if (!_customers.ContainsKey(companyId))
        {
            _customers[companyId] = new List<Customer>();
        }

        _customers[companyId].Add(customer);
        _paidInvoicesByCustomer[customer.Id] = new List<Invoice>
        {
            new(),
            new(),
            new()
        };
    }
}
