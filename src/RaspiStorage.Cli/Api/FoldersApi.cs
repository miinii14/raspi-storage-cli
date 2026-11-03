using System.Net.Http.Json;
using RaspiStorage.Cli.DTOs;

namespace RaspiStorage.Cli.Api;

public class FoldersApi (HttpClient http) : ApiBase(http)
{

    public async Task<FolderDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await Http.GetAsync($"api/folders/{id}", ct);
        await EnsureSuccessAsync(response, ct);


        var json = await ReadRequiredAsync<FolderDto>(response, ct);

        return json;
    }

    public async Task<IEnumerable<FolderDto>> GetByParentIdAsync(Guid? id, CancellationToken ct = default)
    {
        using var response = await Http.GetAsync($"api/folders?parentId={id}", ct);
        await EnsureSuccessAsync(response, ct);

        var json = await ReadRequiredAsync<IEnumerable<FolderDto>>(response, ct);

        return json;
    }

    public async Task<FolderDto> CreateAsync(string name, Guid? parentId, CancellationToken ct = default)
    {
        CreateFolderRequest createFolderRequest = new CreateFolderRequest(name, parentId);

        using var response = await Http.PostAsJsonAsync($"api/folders", createFolderRequest, ct);
        await EnsureSuccessAsync(response, ct);

        var json = await ReadRequiredAsync<FolderDto>(response, ct);

        return json;
    }

    public async Task<FolderDto> RenameAsync(Guid id, string name, CancellationToken ct = default)
    {
        RenameRequest renameRequest = new RenameRequest(name);

        using var response = await Http.PatchAsJsonAsync($"api/folders/rename/{id}", renameRequest, ct);
        await EnsureSuccessAsync(response, ct);

        var json = await ReadRequiredAsync<FolderDto>(response, ct);

        return json;
    }

    public async Task<FolderDto> MoveAsync(Guid id, Guid? folderId, CancellationToken ct = default)
    {
        MoveRequest moveRequest = new MoveRequest(folderId);

        using var response = await Http.PatchAsJsonAsync($"api/folders/move/{id}", moveRequest, ct);
        await EnsureSuccessAsync(response, ct);

        var json = await ReadRequiredAsync<FolderDto>(response, ct);

        return json;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {

        using var response = await Http.DeleteAsync($"api/folders/{id}", ct);
        await EnsureSuccessAsync(response, ct);
    }
}