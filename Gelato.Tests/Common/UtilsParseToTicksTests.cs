namespace Gelato.Tests.Common;

public class UtilsParseToTicksTests
{
    [Theory]
    [InlineData("2:29:00", 89400000000L)]
    [InlineData("02:29:00", 89400000000L)]
    [InlineData("2h29min", 89400000000L)]
    [InlineData("2h 29min", 89400000000L)]
    [InlineData("1h", 36000000000L)]
    [InlineData("90s", 900000000L)]
    [InlineData("45sec", 450000000L)]
    [InlineData("PT90S", 900000000L)]
    [InlineData("1.02:03:04", 937840000000L)]
    public void ParsesSupportedFormats(string input, long expectedTicks)
    {
        Assert.Equal(expectedTicks, Utils.ParseToTicks(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankInput_ReturnsNull(string? input)
    {
        Assert.Null(Utils.ParseToTicks(input));
    }

    [Fact]
    public void Unparseable_ReturnsZeroNotNull()
    {
        // Every strategy fails, and the final regex fallback yields TimeSpan.Zero.
        Assert.Equal(0L, Utils.ParseToTicks("abc"));
    }

    [Fact]
    public void BareNumber_MeansMinutes()
    {
        // A bare integer from an addon is a runtime in minutes, never in days.
        Assert.Equal(TimeSpan.FromMinutes(149).Ticks, Utils.ParseToTicks("149"));
    }

    [Fact]
    public void Iso8601Duration_KeepsMinutes()
    {
        Assert.Equal(new TimeSpan(2, 29, 0).Ticks, Utils.ParseToTicks("PT2H29M"));
    }

    [Theory]
    [InlineData("pt2h29m")]
    [InlineData("Pt2H29m")]
    public void Iso8601Duration_IsCaseInsensitive(string input)
    {
        Assert.Equal(new TimeSpan(2, 29, 0).Ticks, Utils.ParseToTicks(input));
    }
}
