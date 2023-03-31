using System.Web;
using Codat.Demos.InvoiceFinancing.Api.Exceptions;
using Codat.Demos.InvoiceFinancing.Api.Models;

namespace Codat.Demos.InvoiceFinancing.Api.DataClients;

public interface ICodatDataClient
{
    Task<Company> CreateCompanyAsync(string companyName);
    Task<List<Platform>> GetAccountingPlatformsAsync();
    Task<List<Invoice>> GetUnpaidInvoicesAsync(Guid companyId);
    Task<List<Customer>> GetCustomersAsync(Guid companyId);
    Task<List<Invoice>> GetPaidInvoicesForCustomerAsync(Guid companyId, string customerId);
}

public class CodatDataClient : ICodatDataClient
{
    private readonly IHttpClientFactory _clientFactory;

    public CodatDataClient(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    private HttpClient Client => _clientFactory.CreateClient("Codat");

    public async Task<Company> CreateCompanyAsync(string companyName)
    {
        var newCompanyRequestObject = new Company { Name = companyName };
        var response = await Client.PostAsJsonAsync("/companies", newCompanyRequestObject);
        if (!response.IsSuccessStatusCode)
        {
            ThrowDataClientExceptionForHttpResponse(response);
        }

        var company = await response.Content.ReadFromJsonAsync<Company>();
        AssertObjectIsNotNull(company);

        return company!;
    }

    public Task<List<Platform>> GetAccountingPlatformsAsync()
    {
        return ProcessPaginatedResponse<Platform>("/integrations", 250, "sourceType = Accounting");
    }

    public Task<List<Invoice>> GetPaidInvoicesForCustomerAsync(Guid companyId, string customerId)
    {
        return ProcessPaginatedResponse<Invoice>($"/companies/{companyId}/data/invoices", 250, $"status = paid && customerRef.id = {customerId}");
    }

    public Task<List<Invoice>> GetUnpaidInvoicesAsync(Guid companyId)
    {
        return ProcessPaginatedResponse<Invoice>(
            $"/companies/{companyId}/data/invoices",
            250,
            "{status = submitted || status = partiallyPaid} && currency = USD && {amountDue > 50 && amountDue <= 1000}"
        );
    }

    public Task<List<Customer>> GetCustomersAsync(Guid companyId)
    {
        return ProcessPaginatedResponse<Customer>($"/companies/{companyId}/data/customers", 250, string.Empty);
    }

    private async Task<List<T>> ProcessPaginatedResponse<T>(string uri, int pageSize, string query)
    {
        var queryString = string.IsNullOrEmpty(query) ? string.Empty : HttpUtility.UrlEncode($"&query={query}");
        var data = new List<T>();
        var page = 1;
        CodatPaginatedResponse<T> pagedResult;
        do
        {
            pagedResult = await ExecuteGetRequestAsync<CodatPaginatedResponse<T>>($"{uri}?page={page}&pageSize={pageSize}{queryString}");
            data.AddRange(pagedResult.Results);

            page++;
        } while (pagedResult.PageNumber * pagedResult.PageSize < pagedResult.TotalResults);

        return data;
    }

    private async Task<T> ExecuteGetRequestAsync<T>(string endpoint)
    {
        var response = await Client.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
        {
            ThrowDataClientExceptionForHttpResponse(response);
        }

        var body = await response.Content.ReadFromJsonAsync<T>();
        AssertObjectIsNotNull(body);

        return body!;
    }

    private static void ThrowDataClientExceptionForHttpResponse(HttpResponseMessage response)
    {
        throw new CodatDataClientException($"Failed with status code {(int) response.StatusCode} ({response.StatusCode})");
    }

    private static void AssertObjectIsNotNull<T>(T input)
    {
        if (input is null)
        {
            throw new CodatDataClientException("Json object is null");
        }
    }
}
