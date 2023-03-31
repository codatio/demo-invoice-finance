using System.Net;
using Codat.Demos.InvoiceFinancing.Api.DataClients;
using Codat.Demos.InvoiceFinancing.Api.Exceptions;
using Codat.Demos.InvoiceFinancing.Api.Models;
using FluentAssertions;
using Moq;
using SoloX.CodeQuality.Test.Helpers.Http;
using Xunit;

namespace Codat.Demos.InvoiceFinancing.Api.Tests.DataClients;

public class CodatDataClientTests
{
    private const string CodatClientName = "Codat";
    private const string CodatCompanyName = "Test Company";

    public static readonly IEnumerable<object[]> UnsuccessfulStatusCodes = Enum.GetValues(typeof(HttpStatusCode))
        .Cast<HttpStatusCode>()
        .Where(x => x is not (>= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices))
        .Select(x => new object[] { x });

    private readonly ICodatDataClient _client;

    private readonly Company _company = new()
    {
        Id = Guid.NewGuid(),
        Name = CodatCompanyName
    };

    private readonly Mock<IHttpClientFactory> _httpClientFactory = new(MockBehavior.Strict);

    private readonly Platform[] _platforms = { new() { Key = "gbol" } };

    public CodatDataClientTests()
    {
        _client = new CodatDataClient(_httpClientFactory.Object);
    }

    [Fact]
    public async Task CreateCompanyAsync_returns_companyId_when_company_created_successfully()
    {
        SetupCreateCompaniesEndpoint(HttpStatusCode.OK);
        var companyId = await _client.CreateCompanyAsync(CodatCompanyName);
        companyId.Should().BeEquivalentTo(_company);
    }

    [Theory]
    [MemberData(nameof(UnsuccessfulStatusCodes))]
    public async Task CreateCompanyAsync_throws_exception_when_response_code_is_not_success(HttpStatusCode statusCode)
    {
        SetupCreateCompaniesEndpoint(statusCode);
        await TestUnsuccessfulErrorCodes(_client.CreateCompanyAsync(CodatCompanyName), statusCode);
    }

    private async Task TestUnsuccessfulErrorCodes<T>(Task<T> actionTask, HttpStatusCode statusCode)
    {
        var response = async () => await actionTask;
        await response.Should().ThrowAsync<CodatDataClientException>().WithMessage($"Failed with status code {(int) statusCode} ({statusCode})");
    }


    [Fact]
    public async Task CreateCompanyAsync_throws_exception_when_company_returned_is_null()
    {
        var builder = GetMockHttpClientBuilder().WithRequest("/companies", HttpMethod.Post).RespondingJsonContent((Company) null!);

        SetupHttpClientFactory(builder);
        var response = async () => await _client.CreateCompanyAsync(CodatCompanyName);
        await response.Should().ThrowAsync<CodatDataClientException>().WithMessage("Json object is null");
    }

    [Fact]
    public async Task GetAccountingPlatformsAsync_returns_companyId_when_company_created_successfully()
    {
        SetupGetAccountingPlatformsEndpoint(HttpStatusCode.OK);
        var dataConnections = await _client.GetAccountingPlatformsAsync();
        dataConnections.Should().HaveCount(_platforms.Length);
    }

    [Theory]
    [MemberData(nameof(UnsuccessfulStatusCodes))]
    public async Task GetAccountingPlatformsAsync_throws_exception_when_response_code_is_not_success(HttpStatusCode statusCode)
    {
        SetupGetAccountingPlatformsEndpoint(statusCode);
        await TestUnsuccessfulErrorCodes(_client.GetAccountingPlatformsAsync(), statusCode);
    }

    [Fact]
    public async Task GetAccountingPlatformsAsync_throws_exception_when_paginated_data_connections_returned_is_null()
    {
        var builder = GetMockHttpClientBuilder().WithRequest("/integrations", HttpMethod.Get).RespondingJsonContent((CodatPaginatedResponse<Platform>) null!);

        SetupHttpClientFactory(builder);

        var response = async () => await _client.GetAccountingPlatformsAsync();
        await response.Should().ThrowAsync<CodatDataClientException>().WithMessage("Json object is null");
    }

    private void SetupHttpClientFactory(IHttpClientRequestMockBuilder builder)
    {
        _httpClientFactory.Setup(x => x.CreateClient(It.Is<string>(y => y.Equals(CodatClientName, StringComparison.Ordinal)))).Returns(builder.Build());
    }

    private static IHttpClientRequestMockBuilder GetMockHttpClientBuilder()
    {
        return new HttpClientMockBuilder().WithBaseAddress(new Uri("https://expected-website.com"));
    }

    private void SetupCreateCompaniesEndpoint(HttpStatusCode statusCode)
    {
        var builder = GetMockHttpClientBuilder().WithRequest("/companies", HttpMethod.Post).RespondingJsonContent(_company, statusCode);

        SetupHttpClientFactory(builder);
    }

    private void SetupGetAccountingPlatformsEndpoint(HttpStatusCode statusCode)
    {
        var builder = GetMockHttpClientBuilder()
            .WithRequest("/integrations", HttpMethod.Get)
            .RespondingJsonContent(new CodatPaginatedResponse<Platform> { Results = _platforms }, statusCode);

        SetupHttpClientFactory(builder);
    }
}
