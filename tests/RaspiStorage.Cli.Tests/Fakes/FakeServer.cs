using System.Net;
using System.Net.Http.Json;
using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.DTOs;

namespace RaspiStorage.Cli.Tests.Fakes;

public class FakeServer
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private readonly FakeHandler _handler;

    public List<FolderDto> Folders { get; } = [];
    public List<FileDto> Files { get; } = [];
    public IReadOnlyList<string> Requests => _handler.Requests;
    public FoldersApi FoldersApi { get; }
    public FilesApi FilesApi { get; }

    public FakeServer()
    {
        _handler = new FakeHandler(HandleAsync);
        var http = _handler.CreateClient();
        FoldersApi = new FoldersApi(http);
        FilesApi = new FilesApi(http);
    }

    public FolderDto AddFolder(string name, FolderDto? parent = null)
    {
        var folder = new FolderDto(Guid.NewGuid(), OwnerId, parent?.Id, name, DateTime.UtcNow);
        Folders.Add(folder);
        return folder;
    }

    public FileDto AddFile(string name, FolderDto? folder = null, long sizeBytes = 100)
    {
        var file = new FileDto(Guid.NewGuid(), OwnerId, folder?.Id, name, sizeBytes, "text/plain", DateTime.UtcNow, DateTime.UtcNow);
        Files.Add(file);
        return file;
    }

    public FolderDto Folder(string name) => Folders.Single(f => f.Name == name);
    public FileDto File(string name) => Files.Single(f => f.Name == name);

    private async Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request)
    {
        var segments = request.RequestUri!.AbsolutePath.Trim('/').Split('/');
        var queryId = ParseQueryId(request.RequestUri.Query);

        return (request.Method.Method, segments) switch
        {
            ("GET", ["api", "folders"]) => Responses.Json(Folders.Where(f => f.ParentId == queryId).ToList()),
            ("GET", ["api", "files"]) => Responses.Json(Files.Where(f => f.FolderId == queryId).ToList()),
            ("POST", ["api", "folders"]) => CreateFolder(await BodyAsync<CreateFolderRequest>(request)),
            ("PATCH", ["api", "folders", "rename", var id]) => RenameFolder(Guid.Parse(id), (await BodyAsync<RenameRequest>(request)).Name),
            ("PATCH", ["api", "folders", "move", var id]) => MoveFolder(Guid.Parse(id), (await BodyAsync<MoveRequest>(request)).FolderId),
            ("PATCH", ["api", "files", "rename", var id]) => RenameFile(Guid.Parse(id), (await BodyAsync<RenameRequest>(request)).Name),
            ("PATCH", ["api", "files", "move", var id]) => MoveFile(Guid.Parse(id), (await BodyAsync<MoveRequest>(request)).FolderId),
            ("DELETE", ["api", "folders", var id]) => Delete(Folders, f => f.Id == Guid.Parse(id)),
            ("DELETE", ["api", "files", var id]) => Delete(Files, f => f.Id == Guid.Parse(id)),
            _ => Responses.Problem(HttpStatusCode.NotFound, "Route not found", "not_found")
        };
    }

    private HttpResponseMessage CreateFolder(CreateFolderRequest body)
    {
        if (NameTaken(body.ParentId, body.Name))
            return Duplicate(body.Name);

        return Responses.Json(AddFolder(body.Name, Folders.SingleOrDefault(f => f.Id == body.ParentId)), HttpStatusCode.Created);
    }

    private HttpResponseMessage RenameFolder(Guid id, string name) =>
        Update(Folders, id, f => NameTaken(f.ParentId, name) ? null : f with { Name = name }, name);

    private HttpResponseMessage MoveFolder(Guid id, Guid? target) =>
        Update(Folders, id, f => NameTaken(target, f.Name) ? null : f with { ParentId = target }, "");

    private HttpResponseMessage RenameFile(Guid id, string name) =>
        Update(Files, id, f => NameTaken(f.FolderId, name) ? null : f with { Name = name }, name);

    private HttpResponseMessage MoveFile(Guid id, Guid? target) =>
        Update(Files, id, f => NameTaken(target, f.Name) ? null : f with { FolderId = target }, "");

    private static HttpResponseMessage Update<T>(List<T> items, Guid id, Func<T, T?> change, string name) where T : class
    {
        var index = items.FindIndex(item => IdOf(item) == id);
        if (index < 0)
            return Responses.Problem(HttpStatusCode.NotFound, $"{id} not found", "not_found");

        var updated = change(items[index]);
        if (updated is null)
            return Duplicate(name);

        items[index] = updated;
        return Responses.Json(updated);
    }

    private static HttpResponseMessage Delete<T>(List<T> items, Predicate<T> match) =>
        items.RemoveAll(match) > 0
            ? Responses.NoContent()
            : Responses.Problem(HttpStatusCode.NotFound, "Not found", "not_found");

    private bool NameTaken(Guid? parentId, string name) =>
        Folders.Any(f => f.ParentId == parentId && f.Name == name) ||
        Files.Any(f => f.FolderId == parentId && f.Name == name);

    private static HttpResponseMessage Duplicate(string name) =>
        Responses.Problem(HttpStatusCode.Conflict, $"'{name}' already exists", "duplicate");

    private static Guid IdOf<T>(T item) => item switch
    {
        FolderDto f => f.Id,
        FileDto f => f.Id,
        _ => throw new ArgumentOutOfRangeException(nameof(item))
    };

    private static Guid? ParseQueryId(string query)
    {
        var value = query[(query.IndexOf('=') + 1)..];
        return value == "" ? null : Guid.Parse(value);
    }

    private static async Task<T> BodyAsync<T>(HttpRequestMessage request) =>
        (await request.Content!.ReadFromJsonAsync<T>())!;
}
