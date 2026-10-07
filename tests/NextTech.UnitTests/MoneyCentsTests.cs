using NextTech.Application.Common;

namespace NextTech.UnitTests;

public sealed class MoneyCentsTests
{
    [Fact]
    public void FromQuetzales_ConvertsTenQuetzales()
    {
        Assert.Equal(1000, MoneyCents.FromQuetzales(10m));
        Assert.Equal(10.00m, MoneyCents.ToQuetzales(1000));
    }

    [Fact]
    public void FromQuetzales_RejectsZero()
    {
        Assert.Throws<AppValidationException>(() => MoneyCents.FromQuetzales(0m));
    }
}
