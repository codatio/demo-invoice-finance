using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace Codat.Demos.InvoiceFinancing.Api.Tests.WAF;

public static class HttpResponseMessageExtensions
{
    public static async Task<T> ShouldBeSuccessfulWithContent<T>(this HttpResponseMessage response)
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        response.Should().BeSuccessful();

        var content = await response.Content.ReadFromJsonAsync<T>(jsonSerializerOptions);
        content.Should().NotBeNull();
        return content!;
    }
}
