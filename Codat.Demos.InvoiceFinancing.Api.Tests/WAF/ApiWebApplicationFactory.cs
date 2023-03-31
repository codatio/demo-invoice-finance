using System.Data;
using Codat.Demos.InvoiceFinancing.Api.DataClients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Codat.Demos.InvoiceFinancing.Api.Tests.WAF;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(
            services =>
            {
                services.Remove<ICodatDataClient>();
                services.AddSingleton<MockCodatDataClient>();
                services.AddSingleton<ICodatDataClient>(provider => provider.GetRequiredService<MockCodatDataClient>());
            }
        );
    }
}

public static class ServiceExtensions
{
    public static void Remove<T>(this IServiceCollection services)
    {
        if (services.IsReadOnly)
        {
            throw new ReadOnlyException($"{nameof(services)} is read only");
        }

        var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(T));
        if (serviceDescriptor != null)
        {
            services.Remove(serviceDescriptor);
        }
    }
}
