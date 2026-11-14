using RaspiStorage.Cli.Shell;

namespace RaspiStorage.Cli.Tests;

public class ProgressStreamTests
{
    private static byte[] RandomBytes(int count)
    {
        var data = new byte[count];
        Random.Shared.NextBytes(data);
        return data;
    }

    [Fact]
    public async Task CopyToAsync_ReportsAllReadBytes_AndKeepsData()
    {
        var data = RandomBytes(100_000);
        long total = 0;
        var source = new ProgressStream(new MemoryStream(data), n => total += n);
        var destination = new MemoryStream();

        await source.CopyToAsync(destination);

        Assert.Equal(data.Length, total);
        Assert.Equal(data, destination.ToArray());
    }

    [Fact]
    public async Task WriteAsync_ReportsAllWrittenBytes_AndKeepsData()
    {
        var data = RandomBytes(100_000);
        long total = 0;
        var inner = new MemoryStream();
        var stream = new ProgressStream(inner, n => total += n);

        await new MemoryStream(data).CopyToAsync(stream);

        Assert.Equal(data.Length, total);
        Assert.Equal(data, inner.ToArray());
    }

    [Fact]
    public async Task ReadAsync_EmptyStream_ReportsZero()
    {
        var reports = new List<long>();
        var stream = new ProgressStream(new MemoryStream(), reports.Add);

        var read = await stream.ReadAsync(new byte[16]);

        Assert.Equal(0, read);
        Assert.Equal([0L], reports);
    }

    [Fact]
    public void Properties_DelegateToInnerStream()
    {
        var inner = new MemoryStream(new byte[42]);
        var stream = new ProgressStream(inner, _ => { });

        stream.Position = 10;

        Assert.Equal(42, stream.Length);
        Assert.Equal(10, inner.Position);
        Assert.True(stream.CanRead);
        Assert.True(stream.CanSeek);
    }

    [Fact]
    public void Dispose_DisposesInnerStream()
    {
        var inner = new MemoryStream();
        var stream = new ProgressStream(inner, _ => { });

        stream.Dispose();

        Assert.False(inner.CanRead);
    }
}
