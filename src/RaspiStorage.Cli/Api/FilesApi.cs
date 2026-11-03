using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaspiStorage.Cli.DTOs;

namespace RaspiStorage.Cli.Api;

public class FilesApi(HttpClient http) : ApiBase(http) 
{

    public async Task<FileDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await Http.GetAsync($"api/files/{id}", ct);
        await EnsureSuccessAsync(response, ct);
        var json = await ReadRequiredAsync<FileDto>(response, ct);

        return json;
    }

    public async Task<IEnumerable<FileDto>> GetByFolderIdAsync(Guid? folderId, CancellationToken ct = default)
    {
        using var response = await Http.GetAsync($"api/files?folderId={folderId}", ct);
        await EnsureSuccessAsync(response, ct);
        var json = await ReadRequiredAsync<IEnumerable<FileDto>>(response, ct);

        return json;
    } 

    public async Task<FileDto> UploadAsync(Stream content, string fileName, Guid? folderId, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(content);

        form.Add(fileContent, "file", fileName);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await Http.PostAsync($"api/files?folderId={folderId}", form, ct);
        await EnsureSuccessAsync(response, ct);

        var json = await ReadRequiredAsync<FileDto>(response, ct);

        return json;
    }

    public async Task DownloadAsync(Guid id, Stream destination, CancellationToken ct = default)
    {
        using var response = await Http.GetAsync($"api/files/download/{id}", HttpCompletionOption.ResponseHeadersRead, ct);

        await EnsureSuccessAsync(response, ct);

        await using var body = await response.Content.ReadAsStreamAsync(ct);
        await body.CopyToAsync(destination, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await Http.DeleteAsync($"api/files/{id}", ct);

        await EnsureSuccessAsync(response, ct);
    }

    public async Task<FileDto> RenameAsync(Guid id, string name, CancellationToken ct = default)
    {
        RenameRequest renameRequest = new RenameRequest(name);
        using var response = await Http.PatchAsJsonAsync($"api/files/rename/{id}", renameRequest, ct);

        await EnsureSuccessAsync(response, ct);

        var json = await ReadRequiredAsync<FileDto>(response, ct);

        return json;
    }

    public async Task<FileDto> MoveAsync(Guid id, Guid? folderId, CancellationToken ct = default)
    {
        MoveRequest moveRequest = new MoveRequest(folderId);
        using var response = await Http.PatchAsJsonAsync($"api/files/move/{id}", moveRequest, ct);

        await EnsureSuccessAsync(response, ct);

        var json = await ReadRequiredAsync<FileDto>(response, ct);

        return json;
    }
}