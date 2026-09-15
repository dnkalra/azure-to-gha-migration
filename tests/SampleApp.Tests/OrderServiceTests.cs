using System;
using SampleApp.Services;
using Xunit;

namespace SampleApp.Tests;

public class OrderServiceTests
{
    private readonly OrderService _service = new();

    [Fact]
    public void GetOrder_ValidId_ReturnsOrder()
    {
        var result = _service.GetOrder(1);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Enterprise Corp", result.CustomerName);
    }

    [Fact]
    public void GetOrder_InvalidId_ReturnsNull()
    {
        var result = _service.GetOrder(0);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(100.0, 0.1, 10.0)]
    [InlineData(250.5, 0.2, 50.1)]
    [InlineData(0, 0.15, 0)]
    public void CalculateTax_ValidInputs_ReturnsExpectedTax(decimal amount, decimal rate, decimal expected)
    {
        var tax = _service.CalculateTax(amount, rate);
        Assert.Equal(expected, tax);
    }

    [Fact]
    public void CalculateTax_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.CalculateTax(-50, 0.1m));
    }
}
