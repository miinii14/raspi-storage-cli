using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Exceptions;

namespace RaspiStorage.Cli.Api;

public class FilesAPI
{
    private readonly HttpClient _http;

    public FilesAPI (HttpClient http) => _http = http;

    private async Task InterceptExceptions(HttpResponseMessage response, CancellationToken ct)
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

    public async Task<FileDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/files/{id}", ct);
        await InterceptExceptions(response, ct);
        var json = await response.Content.ReadFromJsonAsync<FileDto>(ct) ?? throw new InvalidOperationException("Server returned an empty response");

        return json;
    }

    public async Task<IEnumerable<FileDto>> GetByFolderIdAsync(Guid? folderId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/files?folderId={folderId}", ct);
        await InterceptExceptions(response, ct);
        var json = await response.Content.ReadFromJsonAsync<IEnumerable<FileDto>>(ct) ?? throw new InvalidOperationException("Server returned an empty response");

        return json;
    } 

    public async Task<FileDto> UploadAsync(Stream content, string fileName, Guid? folderId, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(content);

        form.Add(fileContent, "file", fileName);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await _http.PostAsync($"api/files?folderId={folderId}", form, ct);
        await InterceptExceptions(response, ct);

        var json = await response.Content.ReadFromJsonAsync<FileDto>(ct) ?? throw new InvalidOperationException("Server returned an empty response");

        return json;
    }

    public async Task DownloadAsync(Guid id, Stream destination, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/files/download/{id}", HttpCompletionOption.ResponseHeadersRead, ct);

        await InterceptExceptions(response, ct);

        await using var body = await response.Content.ReadAsStreamAsync(ct);
        await body.CopyToAsync(destination, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"api/files/{id}", ct);

        await InterceptExceptions(response, ct);
    }

    public async Task<FileDto> RenameAsync(Guid id, string name, CancellationToken ct = default)
    {
        RenameRequest renameRequest = new RenameRequest(name);
        using var response = await _http.PatchAsJsonAsync($"api/files/rename/{id}", renameRequest, ct);

        await InterceptExceptions(response, ct);

        var json = await response.Content.ReadFromJsonAsync<FileDto>() ?? throw new InvalidOperationException("Server returned an empty response");

        return json;
    }

    public async Task<FileDto> MoveAsync(Guid id, Guid? folderId, CancellationToken ct = default)
    {
        MoveRequest moveRequest = new MoveRequest(folderId);
        using var response = await _http.PatchAsJsonAsync($"api/files/move/{id}", moveRequest, ct);

        await InterceptExceptions(response, ct);

        var json = await response.Content.ReadFromJsonAsync<FileDto>() ?? throw new InvalidOperationException("Server returned an empty response");

        return json;
    }
}