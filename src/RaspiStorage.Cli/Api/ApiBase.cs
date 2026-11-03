using System.Net.Http.Json;
using System.Text.Json;
using RaspiStorage.Cli.Exceptions;

namespace RaspiStorage.Cli.Api;

public abstract class ApiBase(HttpClient http)
{
    protected HttpClient Http { get; } = http;

    protected static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if(response.IsSuccessStatusCode)
            return;

        ApiProblem? problem = null;

        if(response.Content.Headers.ContentType?.MediaType is "application/problem+json" or "application/json")
        {
            try
            {
                problem = await response.Content.ReadFromJsonAsync<ApiProblem>(ct);
            }
            catch (JsonException){  }
        }

        throw new ApiException((int)response.StatusCode, problem);   
    }

    protected static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        return await response.Content.ReadFromJsonAsync<T>(ct)
               ?? throw new InvalidOperationException("Server returned an empty response");
    }
}