using Codat.Demos.InvoiceFinancing.Api.Exceptions;
using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Demos.InvoiceFinancing.Api.Services;
using FluentAssertions;
using Xunit;

namespace Codat.Demos.InvoiceFinancing.Api.Tests.Services;

public class ApplicationStoreTests
{
    private readonly Application _application = new()
    {
        Id = Guid.NewGuid(),
        CodatCompanyId = Guid.NewGuid(),
        Status = ApplicationStatus.Started
    };

    private readonly ApplicationStore _applicationStore = new();

    [Fact]
    public void CreateApplication_sets_only_expected_fields()
    {
        _applicationStore.CreateApplication(_application.Id, _application.CodatCompanyId);

        var application = _applicationStore.GetApplication(_application.Id);

        application.Should().BeEquivalentTo(_application);
        application.Requirements.Should().BeEmpty();
    }

    [Fact]
    public void GetApplication_successfully_retrieves_application_when_multiple_exist()
    {
        var applications = new[]
        {
            _application,
            new()
            {
                Id = Guid.NewGuid(),
                CodatCompanyId = Guid.NewGuid(),
                Status = ApplicationStatus.Started
            }
        };

        foreach (var application in applications)
        {
            _applicationStore.CreateApplication(application.Id, application.CodatCompanyId);
        }

        foreach (var id in applications.Select(x => x.Id))
        {
            var application = _applicationStore.GetApplication(id);
            applications.Should().Contain(x => x.Id == application.Id);
        }
    }

    [Fact]
    public void GetApplication_throws_ApplicationStoreException_when_no_application_exists()
    {
        var missingId = Guid.NewGuid();
        _applicationStore.CreateApplication(_application.Id, _application.CodatCompanyId);

        var action = () => _applicationStore.GetApplication(missingId);

        action.Should().Throw<ApplicationStoreException>().WithMessage($"No application exists with id {missingId}");
    }

    [Fact]
    public void GetApplicationByCompanyId_throws_ApplicationStoreException_when_no_company_exists()
    {
        var missingCompanyId = Guid.NewGuid();
        _applicationStore.CreateApplication(_application.Id, _application.CodatCompanyId);

        var action = () => _applicationStore.GetApplicationByCompanyId(missingCompanyId);

        action.Should().Throw<ApplicationStoreException>().WithMessage($"No application exists for codat company id {missingCompanyId}");
    }

    [Fact]
    public void SetRequirementForCompany_sets_requirements_as_expected()
    {
        var expectation = _application;

        _applicationStore.CreateApplication(_application.Id, _application.CodatCompanyId);

        foreach (var requirement in Enum.GetValues(typeof(ApplicationDataRequirements)).Cast<ApplicationDataRequirements>())
        {
            _applicationStore.AddFulfilledRequirementForCompany(_application.CodatCompanyId, requirement);
            var application = _applicationStore.GetApplication(_application.Id);
            expectation.Requirements.Add(requirement);
            application.Should().BeEquivalentTo(expectation);
        }

        _application.Requirements.Should().HaveSameCount(Enum.GetValues(typeof(ApplicationDataRequirements)).Cast<ApplicationDataRequirements>());
    }
}
