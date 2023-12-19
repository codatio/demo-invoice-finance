using System.Text;
using Codat.Demos.InvoiceFinancing.Api.Exceptions;
using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Demos.InvoiceFinancing.Api.Orchestrators;
using Codat.Demos.InvoiceFinancing.Api.Services;
using Codat.Lending;
using Codat.Platform;

namespace Codat.Demos.InvoiceFinancing.Api;

public static class BindingModule
{

    public static IServiceCollection Bind(this IServiceCollection services, IConfiguration configuration)
    {
        AddCodatHttpClient(services, configuration);

        services.Configure<InvoiceFinancingParameters>(configuration.GetSection("AppSettings:InvoiceFinancingParameters"));

        return services.AddSingleton<IApplicationStore, ApplicationStore>()
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
        var authHeader = $"Basic {encodedApiKey}";

        services.AddSingleton<ICodatPlatform, CodatPlatform>(_ => new CodatPlatform(new(){ AuthHeader = authHeader}));
        services.AddSingleton<ICodatLending, CodatLending>(_ => new CodatLending(new(){ AuthHeader = authHeader}));
    }
}
