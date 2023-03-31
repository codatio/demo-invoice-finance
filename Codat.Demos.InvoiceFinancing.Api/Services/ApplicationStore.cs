using Codat.Demos.InvoiceFinancing.Api.Exceptions;
using Codat.Demos.InvoiceFinancing.Api.Models;

namespace Codat.Demos.InvoiceFinancing.Api.Services;

public interface IApplicationStore
{
    NewApplicationDetails CreateApplication(Guid applicationId, Guid codatCompanyId);
    Application GetApplication(Guid id);
    ApplicationStatus GetApplicationStatus(Guid id);
    void UpdateApplicationStatus(Guid id, ApplicationStatus status);
    void SetAccountingConnectionForCompany(Guid companyId, Guid dataConnectionId);
    Application GetApplicationByCompanyId(Guid companyId);
    void AddFulfilledRequirementForCompany(Guid companyId, ApplicationDataRequirements requirement);
    void AddInvoiceDecisions(Guid id, List<InvoiceDecision> invoiceDecisions);
}

public class ApplicationStore : IApplicationStore
{
    private readonly Dictionary<Guid, Application> _data = new();

    public NewApplicationDetails CreateApplication(Guid applicationId, Guid codatCompanyId)
    {
        var application = new Application
        {
            Id = applicationId,
            CodatCompanyId = codatCompanyId,
            Status = ApplicationStatus.Started
        };
        _data.Add(application.Id, application);

        return new NewApplicationDetails
        {
            Id = application.Id,
            Status = application.Status,
            CodatCompanyId = application.CodatCompanyId
        };
    }

    public Application GetApplication(Guid id)
    {
        return _data.TryGetValue(id, out var result) ? result : throw new ApplicationStoreException($"No application exists with id {id}");
    }

    public ApplicationStatus GetApplicationStatus(Guid id)
    {
        return GetApplication(id).Status;
    }

    public void SetAccountingConnectionForCompany(Guid companyId, Guid dataConnectionId)
    {
        var application = GetApplicationByCompanyId(companyId);

        _data[application.Id] = application with { AccountingConnection = dataConnectionId };
    }

    public void UpdateApplicationStatus(Guid id, ApplicationStatus status)
    {
        var application = GetApplication(id);
        _data[application.Id] = application with { Status = status };
    }

    public Application GetApplicationByCompanyId(Guid companyId)
    {
        var applicationForm = _data.Values.FirstOrDefault(x => x.CodatCompanyId == companyId);
        if (applicationForm is null)
        {
            throw new ApplicationStoreException($"No application exists for codat company id {companyId}");
        }

        return applicationForm;
    }

    public void AddFulfilledRequirementForCompany(Guid companyId, ApplicationDataRequirements requirement)
    {
        var application = GetApplicationByCompanyId(companyId);
        AddToRequirements(application.Id, requirement);
    }

    public void AddInvoiceDecisions(Guid id, List<InvoiceDecision> invoiceDecisions)
    {
        var application = GetApplication(id);
        _data[application.Id] = application with { Decisions = invoiceDecisions };
    }

    private void AddToRequirements(Guid id, ApplicationDataRequirements requirement)
    {
        if (!_data[id].Requirements.Exists(x => x == requirement))
        {
            _data[id].Requirements.Add(requirement);
        }
    }
}
