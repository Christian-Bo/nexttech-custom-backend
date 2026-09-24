using NextTech.Application.Common;
using NextTech.Infrastructure.QR;

namespace NextTech.UnitTests;

public sealed class OrderQrPngTests
{
    [Fact]
    public void Create_ReturnsPngForOrderCode()
    {
        var png = OrderQrPng.Create("ORD-TESTQR01");

        Assert.True(png.Length > 100);
        Assert.Equal(0x89, png[0]);
        Assert.Equal((byte)'P', png[1]);
        Assert.Equal((byte)'N', png[2]);
        Assert.Equal((byte)'G', png[3]);
        Assert.Equal("qr-ORD-TESTQR01.png", OrderQrPng.FileName("ORD-TESTQR01"));
    }

    [Fact]
    public void Create_RejectsEmptyCode()
    {
        Assert.Throws<AppValidationException>(() => OrderQrPng.Create("  "));
    }
}
