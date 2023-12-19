using System.Net;
using System.Runtime.CompilerServices;
using Codat.Demos.InvoiceFinancing.Api.Exceptions;
using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Demos.InvoiceFinancing.Api.Orchestrators;
using Codat.Demos.InvoiceFinancing.Api.Services;
using Codat.Platform;
using Codat.Platform.Models.Operations;
using Codat.Platform.Models.Shared;
using FluentAssertions;
using Moq;
using Xunit;

namespace Codat.Demos.InvoiceFinancing.Api.Tests.Orchestrators;

public class ApplicationOrchestratorTests
{
    private const string ExpectedPlatformKey = "mqjo";
    private readonly Mock<IApplicationStore> _applicationStore = new(MockBehavior.Strict);
    private readonly Mock<ICodatPlatform> _codatPlatform = new(MockBehavior.Strict);
    private readonly Mock<IFinancingProcessor> _financingProcessor = new(MockBehavior.Strict);
    private readonly ApplicationOrchestrator _orchestrator;

    public ApplicationOrchestratorTests()
    {
        _orchestrator = new ApplicationOrchestrator(_applicationStore.Object, _codatPlatform.Object, _financingProcessor.Object);
    }

    public static IEnumerable<object[]> ValidDataTypesAndAssociatedRequirements()
    {
        yield return new object[] { "customers", ApplicationDataRequirements.Customers };
        yield return new object[] { "invoices", ApplicationDataRequirements.Invoices };
    }

    [Fact]
    public async Task CreateApplicationAsync_sets_application_id_and_codat_company_id_in_application_form()
    {
        var codatCompanyId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        var statusCode = HttpStatusCode.Created;
        var companiesClient = new Mock<ICompanies>(MockBehavior.Strict);
        companiesClient.Setup(x => x.CreateAsync(It.IsAny<CompanyRequestBody?>()))
            .ReturnsAsync(new CreateCompanyResponse()
            {
                RawResponse = new HttpResponseMessage(statusCode),
                StatusCode = (int)statusCode,
                Company = new Company() { Id = codatCompanyId.ToString() }
            })
            .Verifiable();

        _codatPlatform.SetupGet(x => x.Companies)
            .Returns(companiesClient.Object)
            .Verifiable();
        
        _applicationStore.Setup(x => x.CreateApplication(It.IsAny<Guid>(), It.Is<Guid>(y => y == codatCompanyId)))
            .Returns(
                new NewApplicationDetails
                {
                    Id = applicationId,
                    CodatCompanyId = codatCompanyId
                }
            )
            .Verifiable();

        var application = await _orchestrator.CreateApplicationAsync();
        application.Id.Should().Be(applicationId);
        application.CodatCompanyId.Should().Be(codatCompanyId);

        companiesClient.Verify();
        companiesClient.VerifyNoOtherCalls();
        
        _codatPlatform.Verify();
        _codatPlatform.VerifyNoOtherCalls();
        
        VerifyApplicationStore();
    }

    [Fact]
    public async Task UpdateCodatDataConnectionAsync_sets_accounting_connection_for_company()
    {
        var codatCompanyId = Guid.NewGuid();

        var alert = CreateDataConnectionStatusAlert(codatCompanyId, "PendingAuth", ExpectedPlatformKey);

        var integrationsClient = GetMockedIntegrationsClient();

        _codatPlatform.SetupGet(x => x.Integrations)
            .Returns(integrationsClient.Object)
            .Verifiable();

        _applicationStore.Setup(
                x => x.SetAccountingConnectionForCompany(It.Is<Guid>(y => y == alert.CompanyId), It.Is<Guid>(y => y == alert.Data.DataConnectionId))
            )
            .Verifiable();

        await _orchestrator.UpdateCodatDataConnectionAsync(alert);

        integrationsClient.Verify();
        integrationsClient.VerifyNoOtherCalls();
        integrationsClient.Verify(x => x.ListAsync(It.IsAny<ListIntegrationsRequest>()), Times.Once);
        
        VerifyCodatPlatformClient();
        VerifyApplicationStore();
    }

    [Fact]
    public async Task UpdateCodatDataConnectionAsync_updates_application_status_when_data_connection_status_is_Linked()
    {
        var codatCompanyId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        var alert = CreateDataConnectionStatusAlert(codatCompanyId, "Linked", ExpectedPlatformKey);

        var integrationsClient = GetMockedIntegrationsClient();

        _codatPlatform.SetupGet(x => x.Integrations)
            .Returns(integrationsClient.Object)
            .Verifiable();

        _applicationStore.Setup(
                x => x.SetAccountingConnectionForCompany(It.Is<Guid>(y => y == alert.CompanyId), It.Is<Guid>(y => y == alert.Data.DataConnectionId))
            )
            .Verifiable();

        _applicationStore.Setup(x => x.GetApplicationByCompanyId(It.Is<Guid>(y => y == codatCompanyId)))
            .Returns(
                new Application
                {
                    Id = applicationId,
                    CodatCompanyId = codatCompanyId
                }
            )
            .Verifiable();

        _applicationStore.Setup(
                x => x.UpdateApplicationStatus(It.Is<Guid>(y => y == applicationId), It.Is<ApplicationStatus>(y => y == ApplicationStatus.AccountsLinked))
            )
            .Verifiable();

        await _orchestrator.UpdateCodatDataConnectionAsync(alert);

        integrationsClient.Verify();
        integrationsClient.VerifyNoOtherCalls();
        integrationsClient.Verify(x => x.ListAsync(It.IsAny<ListIntegrationsRequest>()), Times.Once);
        
        VerifyCodatPlatformClient();
        VerifyApplicationStore();
    }

    [Theory]
    [MemberData(nameof(ValidDataTypesAndAssociatedRequirements))]
    public async Task UpdateDataTypeSyncStatusAsync_updates_requirement_for_required_data_type(string dataType, ApplicationDataRequirements expectedRequirement)
    {
        var codatCompanyId = Guid.NewGuid();
        var dataConnectionId = Guid.NewGuid();

        var application = new Application
        {
            Id = Guid.NewGuid(),
            CodatCompanyId = codatCompanyId,
            AccountingConnection = dataConnectionId
        };

        _applicationStore.Setup(x => x.GetApplicationByCompanyId(It.Is<Guid>(y => y == codatCompanyId))).Returns(application).Verifiable();

        _applicationStore.Setup(
                x => x.AddFulfilledRequirementForCompany(
                    It.Is<Guid>(y => y == codatCompanyId),
                    It.Is<ApplicationDataRequirements>(y => y == expectedRequirement)
                )
            )
            .Verifiable();

        _applicationStore.Setup(x => x.GetApplication(It.IsAny<Guid>())).Returns(application);

        _applicationStore.Setup(x => x.UpdateApplicationStatus(It.IsAny<Guid>(), It.IsAny<ApplicationStatus>()));

        _applicationStore.Setup(x => x.GetApplicationStatus(It.IsAny<Guid>())).Returns(ApplicationStatus.CollectingData);

        var alert = CreateDataSyncCompleteAlert(codatCompanyId, dataConnectionId, dataType);

        await _orchestrator.UpdateDataTypeSyncStatusAsync(alert);

        _applicationStore.Verify(
            x => x.AddFulfilledRequirementForCompany(It.Is<Guid>(y => y == codatCompanyId), It.Is<ApplicationDataRequirements>(y => y == expectedRequirement)),
            Times.AtLeastOnce
        );

        VerifyCodatPlatformClient();
    }

    [Fact]
    public async Task UpdateDataTypeSyncStatusAsync_should_throw_ApplicationOrchestratorException_when_not_accounting_connection_has_been_set()
    {
        var codatCompanyId = Guid.NewGuid();

        _applicationStore.Setup(x => x.GetApplicationByCompanyId(It.Is<Guid>(y => y == codatCompanyId)))
            .Returns(
                new Application
                {
                    Id = Guid.NewGuid(),
                    CodatCompanyId = codatCompanyId,
                    AccountingConnection = null
                }
            )
            .Verifiable();

        var alert = CreateDataSyncCompleteAlert(codatCompanyId);

        var action = () => _orchestrator.UpdateDataTypeSyncStatusAsync(alert);
        await action.Should()
            .ThrowAsync<ApplicationOrchestratorException>()
            .WithMessage($"Cannot update data type sync status as no accounting data connection exists with id {alert.DataConnectionId}");

        VerifyApplicationStore();
        VerifyCodatPlatformClient();
    }

    [Fact]
    public async Task UpdateDataTypeSyncStatusAsync_ignores_data_connections_that_do_not_match_account_data_connection()
    {
        var codatCompanyId = Guid.NewGuid();

        _applicationStore.Setup(x => x.GetApplicationByCompanyId(It.Is<Guid>(y => y == codatCompanyId)))
            .Returns(
                new Application
                {
                    Id = Guid.NewGuid(),
                    CodatCompanyId = codatCompanyId,
                    AccountingConnection = Guid.NewGuid()
                }
            )
            .Verifiable();

        var alert = CreateDataSyncCompleteAlert(codatCompanyId);

        _applicationStore.Setup(x => x.AddFulfilledRequirementForCompany(It.Is<Guid>(y => y == codatCompanyId), It.IsAny<ApplicationDataRequirements>()));

        await _orchestrator.UpdateDataTypeSyncStatusAsync(alert);

        _applicationStore.Verify(
            x => x.AddFulfilledRequirementForCompany(It.Is<Guid>(y => y == codatCompanyId), It.IsAny<ApplicationDataRequirements>()),
            Times.Never
        );

        VerifyCodatPlatformClient();
    }

    [Fact]
    public async Task UpdateDataTypeSyncStatusAsync_sets_application_status_to_CodatProcessingInProgress_when_data_requirements_are_not_met()
    {
        var codatCompanyId = Guid.NewGuid();
        var dataConnectionId = Guid.NewGuid();

        var application = new Application
        {
            Id = Guid.NewGuid(),
            CodatCompanyId = codatCompanyId,
            AccountingConnection = dataConnectionId,
            Requirements = { ApplicationDataRequirements.Invoices }
        };

        _applicationStore.Setup(x => x.GetApplicationByCompanyId(It.IsAny<Guid>())).Returns(application).Verifiable();

        _applicationStore.Setup(x => x.AddFulfilledRequirementForCompany(It.IsAny<Guid>(), It.IsAny<ApplicationDataRequirements>())).Verifiable();

        _applicationStore.Setup(x => x.GetApplication(It.Is<Guid>(y => y == application.Id))).Returns(application).Verifiable();

        _applicationStore.Setup(
                x => x.UpdateApplicationStatus(It.Is<Guid>(y => y == application.Id), It.Is<ApplicationStatus>(y => y == ApplicationStatus.CollectingData))
            )
            .Verifiable();

        _applicationStore.Setup(x => x.GetApplicationStatus(It.Is<Guid>(y => y == application.Id))).Returns(ApplicationStatus.AccountsLinked).Verifiable();

        var alert = CreateDataSyncCompleteAlert(codatCompanyId, dataConnectionId, "invoices");

        await _orchestrator.UpdateDataTypeSyncStatusAsync(alert);

        VerifyApplicationStore();
        VerifyCodatPlatformClient();
    }

    private Mock<IIntegrations> GetMockedIntegrationsClient()
    {
        var statusCode = HttpStatusCode.OK;
        var integrationsClient = new Mock<IIntegrations>(MockBehavior.Strict);
        integrationsClient.Setup(x => x.ListAsync(It.IsAny<ListIntegrationsRequest>()))
            .ReturnsAsync(
                new ListIntegrationsResponse()
                {
                    RawResponse = new HttpResponseMessage(statusCode),
                    StatusCode = (int) statusCode,
                    Integrations = new()
                    {
                        Results = new List<Integration>()
                        {
                            new() { Key = ExpectedPlatformKey },
                            new() { Key = "gbol" }
                        }
                    }
                }
            ).Verifiable();

        return integrationsClient;
    }

    private static CodatDataConnectionStatusAlert CreateDataConnectionStatusAlert(Guid companyId, string newStatus, string platformKey)
    {
        return new CodatDataConnectionStatusAlert
        {
            CompanyId = companyId,
            Data = new CodatDataConnectionStatusData
            {
                DataConnectionId = Guid.NewGuid(),
                NewStatus = newStatus,
                PlatformKey = platformKey
            }
        };
    }

    private static CodatDataSyncCompleteAlert CreateDataSyncCompleteAlert(Guid companyId, Guid? dataConnectionId = null, string dataType = "dataType")
    {
        return new CodatDataSyncCompleteAlert
        {
            CompanyId = companyId,
            DataConnectionId = dataConnectionId ?? Guid.NewGuid(),
            Data = new CodatDataSyncCompleteData { DataType = dataType }
        };
    }

    private void VerifyApplicationStore()
    {
        _applicationStore.Verify();
        _applicationStore.VerifyNoOtherCalls();
    }

    private void VerifyCodatPlatformClient()
    {
        _codatPlatform.Verify();
        _codatPlatform.VerifyNoOtherCalls();
    }
}
