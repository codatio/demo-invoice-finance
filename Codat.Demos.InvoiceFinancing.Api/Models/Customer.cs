namespace Codat.Demos.InvoiceFinancing.Api.Models;

public record Customer
{
    public string Id { get; init; }
    public string RegistrationNumber { get; init; }
    public List<Address> Addresses { get; init; }

    public bool IsUnitedStatesCustomer()
    {
        return Addresses.All(x => x.Country == "United States");
    }
}

public record Address
{
    public string Country { get; init; }
}

public record CustomerRisk
{
    public string CustomerId { get; init; }
    public decimal Risk { get; init; }
}
