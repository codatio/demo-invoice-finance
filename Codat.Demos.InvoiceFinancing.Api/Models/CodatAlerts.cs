namespace Codat.Demos.InvoiceFinancing.Api.Models;

public record CodatDataConnectionStatusAlert : CodatAlertBase<CodatDataConnectionStatusData> { }

public record CodatDataConnectionStatusData
{
    public Guid DataConnectionId { get; init; }
    public string NewStatus { get; init; }
    public string PlatformKey { get; init; }
}

public record CodatDataSyncCompleteAlert : CodatAlertBase<CodatDataSyncCompleteData>
{
    public Guid DataConnectionId { get; init; }
}

public record CodatDataSyncCompleteData
{
    public string DataType { get; init; }
}

public record CodatAlertBase<TData> : CodatAlertBase
{
    public TData Data { get; init; }
}

public record CodatAlertBase
{
    public Guid CompanyId { get; init; }
}
