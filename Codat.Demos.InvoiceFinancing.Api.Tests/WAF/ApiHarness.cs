using System.Text;
using System.Text.Json;
using Codat.Demos.InvoiceFinancing.Api.Models;
using FluentAssertions;

namespace Codat.Demos.InvoiceFinancing.Api.Tests.WAF;

public sealed class ApiHarness : IRootHarness, IDisposable, IAsyncDisposable
{
    public ApiHarness()
    {
        Factory = new ApiWebApplicationFactory();
        CodatDataClientHarness = new CodatDataClientHarness(this);
    }

    public ApiWebApplicationFactory Factory { get; }
    public CodatDataClientHarness CodatDataClientHarness { get; }

    public ValueTask DisposeAsync()
    {
        return Factory.DisposeAsync();
    }

    public void Dispose()
    {
        Factory.Dispose();
    }

    public IServiceProvider Services => Factory.Services;

    public async Task<HttpResponseMessage> Get(string url, Action<HttpClient>? httpClientConfigurator = null)
    {
        using var httpClient = Factory.CreateClient();
        httpClientConfigurator?.Invoke(httpClient);

        var response = await httpClient.GetAsync(url);

        return response;
    }

    public async Task<HttpResponseMessage> Post(string url, object body, Action<HttpClient>? httpClientConfigurator = null)
    {
        using var httpClient = Factory.CreateClient();
        httpClientConfigurator?.Invoke(httpClient);

        var httpContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(url, httpContent);

        return response;
    }

    public async Task<NewApplicationDetails> Start_application()
    {
        var response = await Post("/applications/start", null!);

        var newApplicationDetails = await response.ShouldBeSuccessfulWithContent<NewApplicationDetails>();
        newApplicationDetails.Status.Should().Be(ApplicationStatus.Started);

        return newApplicationDetails;
    }

    public async Task<Guid> Link_application_connection()
    {
        var dataConnectionId = Guid.NewGuid();
        var company = CodatDataClientHarness.GetCreatedCompany();
        var alert = new CodatDataConnectionStatusAlert
        {
            CompanyId = company.Id,
            Data = new CodatDataConnectionStatusData
            {
                DataConnectionId = dataConnectionId,
                NewStatus = "Linked",
                PlatformKey = "mqjo"
            }
        };

        var response = await Post("/webhooks/codat/data-connection-status", alert);
        response.Should().BeSuccessful();

        return dataConnectionId;
    }

    public async Task Complete_application_datatype_sync(Guid dataConnectionId, string dataType)
    {
        var company = CodatDataClientHarness.GetCreatedCompany();
        var alert = new CodatDataSyncCompleteAlert
        {
            CompanyId = company.Id,
            DataConnectionId = dataConnectionId,
            Data = new CodatDataSyncCompleteData { DataType = dataType }
        };

        var response = await Post("/webhooks/codat/datatype-sync-complete", alert);
        response.Should().BeSuccessful();
    }
}
