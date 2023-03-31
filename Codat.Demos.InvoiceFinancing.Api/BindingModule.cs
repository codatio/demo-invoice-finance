using System.Text;
using Codat.Demos.InvoiceFinancing.Api.DataClients;
using Codat.Demos.InvoiceFinancing.Api.Exceptions;
using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Demos.InvoiceFinancing.Api.Orchestrators;
using Codat.Demos.InvoiceFinancing.Api.Services;
using Microsoft.Net.Http.Headers;

namespace Codat.Demos.InvoiceFinancing.Api;

public static class BindingModule
{
    private const string CodatUrl = "https://api.codat.io";
    private const string ContentType = "application/json";

    public static IServiceCollection Bind(this IServiceCollection services, IConfiguration configuration)
    {
        AddCodatHttpClient(services, configuration);

        services.Configure<InvoiceFinancingParameters>(configuration.GetSection("AppSettings:InvoiceFinancingParameters"));

        return services.AddSingleton<IApplicationStore, ApplicationStore>()
            .AddSingleton<ICodatDataClient, CodatDataClient>()
            .AddSingleton<IApplicationOrchestrator, ApplicationOrchestrator>()
            .AddSingleton<IFinancingProcessor, FinancingProcessor>()
            .AddSingleton<ICustomerRiskAssessor, CustomerRiskAssessor>()
            .AddSingleton<IInvoiceFinanceAssessor, InvoiceFinanceAssessor>();
    }

    private static void AddCodatHttpClient(IServiceCollection services, IConfiguration configuration)
    {
        const string apiKeyParam = "AppSettings:CodatApiKey";
        var apiKey = configuration.GetSection(apiKeyParam).Value;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ConfigurationMissingException(apiKeyParam);
        }

        var encodedApiKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));

        services.AddHttpClient(
            "Codat",
            httpClient =>
            {
                httpClient.BaseAddress = new Uri(CodatUrl);
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, ContentType);
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Basic {encodedApiKey}");
            }
        );
    }
}
