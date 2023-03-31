using Codat.Demos.InvoiceFinancing.Api.DataClients;
using Codat.Demos.InvoiceFinancing.Api.Exceptions;
using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Demos.InvoiceFinancing.Api.Services;

namespace Codat.Demos.InvoiceFinancing.Api.Orchestrators;

public interface IApplicationOrchestrator
{
    Task<NewApplicationDetails> CreateApplicationAsync();
    Application GetApplication(Guid id);
    Task UpdateCodatDataConnectionAsync(CodatDataConnectionStatusAlert alert);
    Task UpdateDataTypeSyncStatusAsync(CodatDataSyncCompleteAlert alert);
}

public class ApplicationOrchestrator : IApplicationOrchestrator
{
    private static readonly ApplicationDataRequirements[] ApplicationRequirements =
        Enum.GetValues(typeof(ApplicationDataRequirements)).Cast<ApplicationDataRequirements>().ToArray();

    private readonly IApplicationStore _applicationStore;
    private readonly ICodatDataClient _codatDataClient;
    private readonly IFinancingProcessor _financingProcessor;

    public ApplicationOrchestrator(IApplicationStore applicationStore, ICodatDataClient codatDataClient, IFinancingProcessor financingProcessor)
    {
        _applicationStore = applicationStore;
        _codatDataClient = codatDataClient;
        _financingProcessor = financingProcessor;
    }

    public async Task<NewApplicationDetails> CreateApplicationAsync()
    {
        var applicationId = Guid.NewGuid();
        var company = await _codatDataClient.CreateCompanyAsync(applicationId.ToString());
        return _applicationStore.CreateApplication(applicationId, company.Id);
    }

    public Application GetApplication(Guid id)
    {
        try
        {
            return _applicationStore.GetApplication(id);
        }
        catch (ApplicationStoreException e)
        {
            throw new ApplicationOrchestratorException(e.Message, e);
        }
    }

    public async Task UpdateCodatDataConnectionAsync(CodatDataConnectionStatusAlert alert)
    {
        var isAccountingPlatform = await IsAccountingPlatformAsync(alert.Data.PlatformKey);
        if (isAccountingPlatform)
        {
            _applicationStore.SetAccountingConnectionForCompany(alert.CompanyId, alert.Data.DataConnectionId);
            if (alert.Data.NewStatus.Equals("Linked", StringComparison.Ordinal))
            {
                var application = _applicationStore.GetApplicationByCompanyId(alert.CompanyId);
                _applicationStore.UpdateApplicationStatus(application.Id, ApplicationStatus.AccountsLinked);
            }
        }
    }

    public async Task UpdateDataTypeSyncStatusAsync(CodatDataSyncCompleteAlert alert)
    {
        var application = _applicationStore.GetApplicationByCompanyId(alert.CompanyId);
        if (application.AccountingConnection is null)
        {
            throw new ApplicationOrchestratorException(
                $"Cannot update data type sync status as no accounting data connection exists with id {alert.DataConnectionId}"
            );
        }

        if (application.AccountingConnection != alert.DataConnectionId)
        {
            return;
        }

        var requirement = GetRequirementByDataType(alert.Data.DataType);
        if (requirement is null)
        {
            return;
        }

        _applicationStore.AddFulfilledRequirementForCompany(alert.CompanyId, requirement.Value);

        await TryProcessFinancingAsync(application.Id);
    }

    private static ApplicationDataRequirements? GetRequirementByDataType(string dataType)
    {
        return dataType switch
        {
            "customers" => ApplicationDataRequirements.Customers,
            "invoices" => ApplicationDataRequirements.Invoices,
            _ => null
        };
    }

    private async Task TryProcessFinancingAsync(Guid id)
    {
        UpdateApplicationStatusGivenRequirements(id);
        if (_applicationStore.GetApplicationStatus(id) == ApplicationStatus.DataCollectionComplete)
        {
            await _financingProcessor.ProcessFinancingForApplication(id);
        }
    }

    private void UpdateApplicationStatusGivenRequirements(Guid id)
    {
        var updatedApplication = _applicationStore.GetApplication(id);
        var requirementsMet = ApplicationRequirements.All(x => updatedApplication.Requirements.Any(y => x == y));
        var status = requirementsMet ? ApplicationStatus.DataCollectionComplete : ApplicationStatus.CollectingData;
        _applicationStore.UpdateApplicationStatus(id, status);
    }

    private async Task<bool> IsAccountingPlatformAsync(string platformKey)
    {
        var platforms = await _codatDataClient.GetAccountingPlatformsAsync();
        return platforms.Any(x => x.Key == platformKey);
    }
}
