// Unit/Models/DecimalExtensionsTests.cs
using FluentAssertions;

using HitBTC.Connector.Core.Models;

using Xunit;

namespace HitBTC.Connector.Tests.Unit.Models;

public class DecimalExtensionsTests
{
    [Theory]
    [InlineData(100, "100")]
    [InlineData(100.5, "100.5")]
    [InlineData(0.00001, "0.00001")]
    [InlineData(0.123456789, "0.123456789")]
    [InlineData(50000.50, "50000.5")]
    [InlineData(0, "0")]
    [InlineData(1.0, "1")]
    [InlineData(10.10, "10.1")]
    [InlineData(0.1, "0.1")]
    [InlineData(0.01, "0.01")]
    [InlineData(0.001, "0.001")]
    [InlineData(0.0001, "0.0001")]
    public void ToApiString_ShouldFormatCorrectly(decimal input, string expected)
    {
        // Act
        var result = input.ToApiString();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToApiString_ShouldNotUseScientificNotation()
    {
        // Arrange
        var smallValue = 0.000000001m;
        var verySmallValue = 0.0000000000001m;
        var largeValue = 1000000000000m;

        // Act
        var smallResult = smallValue.ToApiString();
        var verySmallResult = verySmallValue.ToApiString();
        var largeResult = largeValue.ToApiString();

        // Assert
        smallResult.Should().NotContainAny("E", "e");
        smallResult.Should().Be("0.000000001");

        verySmallResult.Should().NotContainAny("E", "e");

        largeResult.Should().NotContainAny("E", "e");
        largeResult.Should().Be("1000000000000");
    }

    [Fact]
    public void ToApiString_ShouldRemoveTrailingZeros()
    {
        // Arrange
        var value = 100.50000m;

        // Act
        var result = value.ToApiString();

        // Assert
        result.Should().Be("100.5");
    }

    [Fact]
    public void ToApiString_ShouldUseInvariantCulture()
    {
        // Arrange
        var value = 1234.56m;

        // Act
        var result = value.ToApiString();

        // Assert
        result.Should().Be("1234.56"); // Not "1234,56" or "1,234.56"
    }

    [Fact]
    public void ToApiString_EdgeCases()
    {
        // Очень большие числа
        var big = 99999999999999999999m;
        big.ToApiString().Should().NotContainAny("E", "e");

        // Очень маленькие числа
        var tiny = 0.00000000000000000001m;
        tiny.ToApiString().Should().NotContainAny("E", "e");

        // Отрицательные
        var negative = -123.456m;
        negative.ToApiString().Should().Be("-123.456");

        // Zero
        0m.ToApiString().Should().Be("0");
    }

    [Theory]
    [InlineData(0.000001)]
    [InlineData(0.0000001)]
    [InlineData(0.00000001)]
    [InlineData(0.000000001)]
    [InlineData(0.0000000001)]
    public void ToApiString_SmallDecimals_ShouldNotUseScientificNotation(decimal value)
    {
        // Act
        var result = value.ToApiString();

        // Assert
        result.Should().NotContainAny("E", "e",
            $"Value {value} should not be formatted with scientific notation");
        result.Should().StartWith("0.");
    }
}