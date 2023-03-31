using System.Net;
using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Demos.InvoiceFinancing.Api.Tests.WAF;
using FluentAssertions;
using Xunit;

namespace Codat.Demos.InvoiceFinancing.Api.Tests.Controllers;

public class ApplicationControllerTests
{
    private readonly ApiHarness _harness = new();

    [Fact]
    public async Task Should_create_application()
    {
        var newApplication = await _harness.Start_application();

        newApplication.Status.Should().Be(ApplicationStatus.Started);
    }

    [Fact]
    public async Task Should_get_created_application()
    {
        var newApplication = await _harness.Start_application();

        var response = await _harness.Get($"/applications/{newApplication.Id}");

        var application = await response.ShouldBeSuccessfulWithContent<Application>();
        application.Status.Should().Be(newApplication.Status);
    }

    [Fact]
    public async Task Should_404_with_no_created_application()
    {
        var response = await _harness.Get($"/applications/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_get_updated_application_when_connection_linked()
    {
        var newApplication = await _harness.Start_application();
        await _harness.Link_application_connection();

        var response = await _harness.Get($"/applications/{newApplication.Id}");

        var application = await response.ShouldBeSuccessfulWithContent<Application>();
        application.Status.Should().Be(ApplicationStatus.AccountsLinked);
    }

    [Fact]
    public async Task Should_get_updated_application_when_first_datatype_complete()
    {
        var newApplication = await _harness.Start_application();
        var dataConnectionId = await _harness.Link_application_connection();
        await _harness.Complete_application_datatype_sync(dataConnectionId, "invoices");

        var response = await _harness.Get($"/applications/{newApplication.Id}");

        var application = await response.ShouldBeSuccessfulWithContent<Application>();
        application.Status.Should().Be(ApplicationStatus.CollectingData);
    }
}
