using System.Globalization;
using RaspiStorage.Cli.Shell;

namespace RaspiStorage.Cli.Tests;

public class FormatTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1, "1 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(31457280, "30 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1099511627776, "1 TB")]
    public void Size_FormatsWithLargestFittingUnit(long bytes, string expected)
    {
        Assert.Equal(expected, Format.Size(bytes));
    }

    [Fact]
    public void Size_AbovePetabyte_StaysInTerabytes()
    {
        Assert.Equal("2048 TB", Format.Size(2L * 1024 * 1024 * 1024 * 1024 * 1024));
    }

    [Fact]
    public void Size_DoesNotDependOnCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("uk-UA");

            Assert.Equal("1.5 KB", Format.Size(1536));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
