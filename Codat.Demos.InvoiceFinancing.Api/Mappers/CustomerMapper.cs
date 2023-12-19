using Codat.Demos.InvoiceFinancing.Api.Models;
using Codat.Lending.Models.Shared;

namespace Codat.Demos.InvoiceFinancing.Api.Mappers;

public class CustomerMapper
{
    public static Customer MapToDomainModel(AccountingCustomer data) =>
        new()
        {
            Id = data.Id,
            RegistrationNumber = data.RegistrationNumber,
            Addresses = data.Addresses.Select(x => new Address() { Country = x.Country }).ToList()
        };
}