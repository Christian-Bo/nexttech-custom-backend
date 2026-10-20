using NextTech.Application.Common;

namespace NextTech.UnitTests;

public sealed class GuatemalaPhoneNumberTests
{
    [Theory]
    [InlineData("55550101", "50255550101")]
    [InlineData("50255550101", "50255550101")]
    [InlineData("+502 5555-0101", "50255550101")]
    [InlineData("5555 0101", "50255550101")]
    [InlineData("(5555) 0101", "50255550101")]
    public void TryNormalizeForWhatsApp_ValidGuatemalaNumber_ReturnsProviderFormat(
        string input,
        string expected)
    {
        Assert.True(GuatemalaPhoneNumber.TryNormalizeForWhatsApp(input, out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("5555010")]
    [InlineData("5025555010")]
    [InlineData("50355550101")]
    [InlineData("+1 55550101")]
    [InlineData("5555abc0101")]
    [InlineData("5555+0101")]
    [InlineData("502555501011")]
    public void TryNormalizeForWhatsApp_InvalidNumber_IsRejected(string input)
    {
        Assert.False(GuatemalaPhoneNumber.TryNormalizeForWhatsApp(input, out var actual));
        Assert.Equal(string.Empty, actual);
    }
}
