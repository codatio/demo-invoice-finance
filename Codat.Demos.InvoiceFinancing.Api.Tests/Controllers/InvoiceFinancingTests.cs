using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Demos.InvoiceFinancing.Api.Tests.WAF;
using FluentAssertions;
using Xunit;

namespace Codat.Demos.InvoiceFinancing.Api.Tests.Controllers;

public class InvoiceFinancingTests
{
    private readonly DateTime _today = DateTime.Today;
    private readonly ApiHarness _harness = new();

    [Fact]
    public async Task Should_process_financing()
    {
        var newApplication = await _harness.Start_application();
        Setup_data();

        var dataConnectionId = await _harness.Link_application_connection();
        await _harness.Complete_application_datatype_sync(dataConnectionId, "invoices");
        await _harness.Complete_application_datatype_sync(dataConnectionId, "customers");

        var response = await _harness.Get($"/applications/{newApplication.Id}");

        var application = await response.ShouldBeSuccessfulWithContent<Application>();
        application.Status.Should().Be(ApplicationStatus.Complete);
        application.Decisions.Should()
        .BeEquivalentTo(
            new List<InvoiceDecision>
            {
                new()
                {
                    InvoiceId = "ui1",
                    InvoiceNo = "ui1",
                    AmountDue = 200,
                    OfferAmount = 180,
                    Rate = 2.3m
                }
            }
        );
        application.Decisions.Should().NotContain(x => x.InvoiceId == "ui2");
        application.Decisions.Should().NotContain(x => x.InvoiceId == "ui3");
    }

    private void Setup_data()
    {
        var company = _harness.CodatDataClientHarness.GetCreatedCompany();
        var customer1 = Setup_customer("c1");
        var customer2 = Setup_customer("c2");

        _harness.CodatDataClientHarness.SetupUnpaidInvoices(
            company.Id,
            new List<Invoice>
            {
                new()
                {
                    Id = "ui1",
                    InvoiceNumber = "ui1",
                    AmountDue = 200,
                    DueDate = _today.AddDays(20),
                    IssueDate = _today.AddDays(-10),
                    CustomerRef = customer1
                },
                new()
                {
                    Id = "ui2",
                    InvoiceNumber = "ui2",
                    AmountDue = 500,
                    DueDate = _today.AddDays(20),
                    IssueDate = _today.AddDays(-10),
                    CustomerRef = customer2
                },
                new()
                {
                    Id = "ui3",
                    InvoiceNumber = "ui3",
                    AmountDue = 900,
                    DueDate = _today.AddDays(20),
                    IssueDate = _today.AddDays(-10),
                    CustomerRef = customer2
                }
            }
        );
    }

    private Customer Setup_customer(string id)
    {
        var company = _harness.CodatDataClientHarness.GetCreatedCompany();

        var customer = new Customer
        {
            Id = id,
            RegistrationNumber = id,
            Addresses = new List<Address> { new() { Country = "US" } }
        };
        _harness.CodatDataClientHarness.SetupCustomer(company.Id, customer);

        return customer;
    }
}
