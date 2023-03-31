using Codat.Demos.InvoiceFinancing.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Codat.Demos.InvoiceFinancing.Api.Tests.WAF;

public class CodatDataClientHarness
{
    private readonly IRootHarness _harness;

    public CodatDataClientHarness(IRootHarness harness)
    {
        _harness = harness;
    }

    public Company GetCreatedCompany()
    {
        using var serviceScope = _harness.Services.CreateScope();
        var mockCodatDataClient = serviceScope.ServiceProvider.GetRequiredService<MockCodatDataClient>();

        return mockCodatDataClient.GetCreatedCompany();
    }

    public void SetupUnpaidInvoices(Guid companyId, List<Invoice> unpaidInvoices)
    {
        using var serviceScope = _harness.Services.CreateScope();
        var mockCodatDataClient = serviceScope.ServiceProvider.GetRequiredService<MockCodatDataClient>();

        mockCodatDataClient.SetupUnpaidInvoices(companyId, unpaidInvoices);
    }

    public void SetupCustomer(Guid companyId, Customer customer)
    {
        using var serviceScope = _harness.Services.CreateScope();
        var mockCodatDataClient = serviceScope.ServiceProvider.GetRequiredService<MockCodatDataClient>();

        mockCodatDataClient.SetupCustomer(companyId, customer);
    }
}
