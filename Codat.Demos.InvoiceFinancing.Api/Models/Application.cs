using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Codat.Demos.InvoiceFinancing.Api.Models;

public enum ApplicationStatus
{
    Started,
    AccountsLinked,
    CollectingData,
    DataCollectionComplete,
    Processing,
    ProcessingError,
    Complete
}

public enum ApplicationDataRequirements
{
    Invoices,
    Customers
}

public record NewApplicationDetails
{
    /// <summary>
    ///     Unique application identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    ///     The current status of the application.
    /// </summary>
    public ApplicationStatus Status { get; init; }

    /// <summary>
    ///     Codat's hosted link URI. This allows applicants to connect their accounting platform.
    /// </summary>
    /// <example>https://link.codat.io/company/{codatCompanyId}</example>
    [MinLength(1)]
    public string LinkUrl => $"https://link.codat.io/company/{CodatCompanyId}";

    [JsonIgnore] public Guid CodatCompanyId { get; init; }
}

public record Application
{
    [JsonIgnore] public Guid Id { get; init; }
    [JsonIgnore] public Guid CodatCompanyId { get; init; }
    [JsonIgnore] public Guid? AccountingConnection { get; init; }
    [JsonIgnore] public List<ApplicationDataRequirements> Requirements { get; } = new();

    /// <summary>
    ///     The current status of the application.
    /// </summary>
    public ApplicationStatus Status { get; init; }

    /// <summary>
    ///     The financing decision for each invoice assessed as low risk
    /// </summary>
    public List<InvoiceDecision>? Decisions { get; init; }
}
