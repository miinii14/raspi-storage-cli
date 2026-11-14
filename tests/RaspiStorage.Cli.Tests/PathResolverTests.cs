using RaspiStorage.Cli.Exceptions;
using RaspiStorage.Cli.Shell.Commands;

namespace RaspiStorage.Cli.Tests;

public class PathResolverTests
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [Theory]
    [InlineData("cat.jpg", "", "cat.jpg")]
    [InlineData("/cat.jpg", "/", "cat.jpg")]
    [InlineData("a/b/cat.jpg", "a/b/", "cat.jpg")]
    [InlineData("/a/b/cat.jpg", "/a/b/", "cat.jpg")]
    [InlineData("../cat.jpg", "../", "cat.jpg")]
    [InlineData("a//cat.jpg", "a//", "cat.jpg")]
    public void Split_ValidPath_ReturnsParentAndName(string path, string expectedParent, string expectedName)
    {
        var (parent, name) = PathResolver.Split(path, "rm");

        Assert.Equal(expectedParent, parent);
        Assert.Equal(expectedName, name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("Photos/")]
    [InlineData("a/b/")]
    public void Split_NoName_ThrowsWithCommandTitle(string path)
    {
        var ex = Assert.Throws<CliException>(() => PathResolver.Split(path, "mkdir"));

        Assert.Equal("mkdir", ex.Title);
    }

    [Fact]
    public void ResolveLocal_Tilde_ReturnsHome()
    {
        Assert.Equal(Home, PathResolver.ResolveLocal("~"));
    }

    [Fact]
    public void ResolveLocal_TildeSlash_ExpandsToHome()
    {
        var expected = Path.Combine(Home, "docs", "a.txt");

        Assert.Equal(expected, PathResolver.ResolveLocal("~/docs/a.txt"));
    }

    [Fact]
    public void ResolveLocal_RelativePath_ResolvesAgainstCurrentDirectory()
    {
        var expected = Path.Combine(Environment.CurrentDirectory, "a.txt");

        Assert.Equal(expected, PathResolver.ResolveLocal("a.txt"));
    }

    [Fact]
    public void ResolveLocal_AbsolutePath_IsNormalized()
    {
        Assert.Equal("/tmp/a.txt", PathResolver.ResolveLocal("/tmp/x/../a.txt"));
    }

    [Fact]
    public void ResolveLocal_TildeFollowedByName_IsNotExpanded()
    {
        var expected = Path.Combine(Environment.CurrentDirectory, "~bob", "a.txt");

        Assert.Equal(expected, PathResolver.ResolveLocal("~bob/a.txt"));
    }

    [Fact]
    public void ToPath_EmptyChain_ReturnsRoot()
    {
        Assert.Equal("/", PathResolver.ToPath([]));
    }
}
