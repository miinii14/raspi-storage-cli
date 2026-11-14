using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Exceptions;
using RaspiStorage.Cli.Shell;
using RaspiStorage.Cli.Shell.Commands;
using RaspiStorage.Cli.Tests.Fakes;

namespace RaspiStorage.Cli.Tests;

public class CommandTests
{
    private readonly FakeServer _server = new();
    private readonly FolderDto _a;
    private readonly FolderDto _b;
    private readonly FolderDto _x;
    private readonly FileDto _notes;

    public CommandTests()
    {
        _a = _server.AddFolder("a");
        _b = _server.AddFolder("b", _a);
        _x = _server.AddFolder("x");
        _notes = _server.AddFile("notes.txt", _a);
    }

    private static Task Run(ICommand command, Context ctx, string line) =>
        command.ExecuteAsync(line.Split(' ', StringSplitOptions.RemoveEmptyEntries), ctx, CancellationToken.None);

    private Context InB() => new() { Folders = [_a, _b] };

    [Fact]
    public async Task Cd_ValidPath_ChangesCurrentFolder()
    {
        var ctx = new Context();

        await Run(new CdCommand(_server.FoldersApi), ctx, "a/b");

        Assert.Equal("/a/b", ctx.Path);
    }

    [Fact]
    public async Task Cd_UnknownPath_KeepsCurrentFolder()
    {
        var ctx = InB();

        await Assert.ThrowsAsync<CliException>(() => Run(new CdCommand(_server.FoldersApi), ctx, "/nope"));

        Assert.Equal("/a/b", ctx.Path);
    }

    [Fact]
    public async Task Rename_AncestorOfCurrentFolder_UpdatesPrompt()
    {
        var ctx = InB();

        await Run(new RenameCommand(_server.FoldersApi, _server.FilesApi), ctx, "/a z");

        Assert.Equal("/z/b", ctx.Path);
        Assert.Equal("z", _server.Folders.Single(f => f.Id == _a.Id).Name);
    }

    [Fact]
    public async Task Rename_ToExistingName_ThrowsAndKeepsPrompt()
    {
        var ctx = InB();

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            Run(new RenameCommand(_server.FoldersApi, _server.FilesApi), ctx, "/a x"));

        Assert.Equal(409, ex.Status);
        Assert.Equal("/a/b", ctx.Path);
    }

    [Fact]
    public async Task Rename_File_RenamesOnServer()
    {
        await Run(new RenameCommand(_server.FoldersApi, _server.FilesApi), new Context(), "a/notes.txt todo.txt");

        Assert.Equal("todo.txt", _server.Files.Single(f => f.Id == _notes.Id).Name);
    }

    [Theory]
    [InlineData("/a /x")]
    [InlineData("/a/b /x")]
    public async Task Mv_CurrentFolderOrAncestor_IsBlockedWithoutRequest(string line)
    {
        var ex = await Assert.ThrowsAsync<CliException>(() =>
            Run(new MvCommand(_server.FoldersApi, _server.FilesApi), InB(), line));

        Assert.Equal("mv", ex.Title);
        Assert.DoesNotContain(_server.Requests, r => r.StartsWith("PATCH"));
    }

    [Fact]
    public async Task Mv_File_MovesToTarget()
    {
        await Run(new MvCommand(_server.FoldersApi, _server.FilesApi), new Context(), "a/notes.txt /x");

        Assert.Equal(_x.Id, _server.Files.Single(f => f.Id == _notes.Id).FolderId);
        Assert.Contains($"PATCH /api/files/move/{_notes.Id}", _server.Requests);
    }

    [Fact]
    public async Task Mv_FolderToRoot_SetsNullParent()
    {
        await Run(new MvCommand(_server.FoldersApi, _server.FilesApi), new Context(), "a/b /");

        Assert.Null(_server.Folders.Single(f => f.Id == _b.Id).ParentId);
    }

    [Fact]
    public async Task Mv_UnknownName_ThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<CliException>(() =>
            Run(new MvCommand(_server.FoldersApi, _server.FilesApi), new Context(), "a/nope /x"));

        Assert.Equal("'nope' not found.", ex.Message);
    }

    [Fact]
    public async Task Mkdir_CreatesFolderInResolvedParent()
    {
        await Run(new MkDirCommand(_server.FoldersApi), new Context(), "a/b/new");

        Assert.Equal(_b.Id, _server.Folder("new").ParentId);
    }

    [Fact]
    public async Task Rm_File_DeletesOnServer()
    {
        await Run(new RmCommand(_server.FoldersApi, _server.FilesApi), new Context(), "a/notes.txt");

        Assert.DoesNotContain(_server.Files, f => f.Id == _notes.Id);
    }

    [Fact]
    public async Task Rm_CurrentFolderAncestor_IsBlockedBeforeConfirm()
    {
        var ex = await Assert.ThrowsAsync<CliException>(() =>
            Run(new RmCommand(_server.FoldersApi, _server.FilesApi), InB(), "/a"));

        Assert.Equal("rm", ex.Title);
        Assert.DoesNotContain(_server.Requests, r => r.StartsWith("DELETE"));
    }

    [Theory]
    [InlineData("mv", "a")]
    [InlineData("rename", "a")]
    [InlineData("rm", "")]
    public async Task WrongArgumentCount_ThrowsUsage(string name, string line)
    {
        ICommand command = name switch
        {
            "mv" => new MvCommand(_server.FoldersApi, _server.FilesApi),
            "rename" => new RenameCommand(_server.FoldersApi, _server.FilesApi),
            _ => new RmCommand(_server.FoldersApi, _server.FilesApi)
        };

        var ex = await Assert.ThrowsAsync<CliException>(() => Run(command, new Context(), line));

        Assert.Equal($"Usage: {command.Usage}", ex.Message);
        Assert.Empty(_server.Requests);
    }
}
