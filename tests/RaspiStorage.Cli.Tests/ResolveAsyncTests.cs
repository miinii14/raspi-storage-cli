using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Exceptions;
using RaspiStorage.Cli.Shell;
using RaspiStorage.Cli.Shell.Commands;
using RaspiStorage.Cli.Tests.Fakes;

namespace RaspiStorage.Cli.Tests;

public class ResolveAsyncTests
{
    private readonly FakeServer _server = new();
    private readonly FolderDto _a;
    private readonly FolderDto _b;

    public ResolveAsyncTests()
    {
        _a = _server.AddFolder("a");
        _b = _server.AddFolder("b", _a);
        _server.AddFolder("x");
    }

    private Task<List<FolderDto>> Resolve(string path, params FolderDto[] current) =>
        PathResolver.ResolveAsync(path, new Context { Folders = [.. current] }, CancellationToken.None, _server.FoldersApi, "cd");

    [Fact]
    public async Task RelativePath_FromRoot_ResolvesChain()
    {
        Assert.Equal([_a, _b], await Resolve("a/b"));
    }

    [Fact]
    public async Task AbsolutePath_IgnoresCurrentFolder()
    {
        Assert.Equal([_a], await Resolve("/a", _a, _b));
    }

    [Fact]
    public async Task DotDot_GoesUpOneLevel()
    {
        Assert.Equal([_a], await Resolve("..", _a, _b));
    }

    [Theory]
    [InlineData("../..")]
    [InlineData("../../..")]
    [InlineData("/")]
    public async Task CannotGoAboveRoot(string path)
    {
        Assert.Empty(await Resolve(path, _a, _b));
    }

    [Fact]
    public async Task Dot_StaysInPlace()
    {
        Assert.Equal([_a, _b], await Resolve("./b", _a));
    }

    [Fact]
    public async Task UnknownFolder_ThrowsWithName()
    {
        var ex = await Assert.ThrowsAsync<CliException>(() => Resolve("a/nope"));

        Assert.Equal("cd", ex.Title);
        Assert.Equal("Folder 'nope' not found.", ex.Message);
    }

    [Fact]
    public async Task DoesNotMutateContext()
    {
        var ctx = new Context { Folders = [_a] };

        await PathResolver.ResolveAsync("b", ctx, CancellationToken.None, _server.FoldersApi, "cd");

        Assert.Equal([_a], ctx.Folders);
    }

    [Fact]
    public async Task DotDot_DoesNotCallServer()
    {
        await Resolve("..", _a, _b);

        Assert.Empty(_server.Requests);
    }
}
