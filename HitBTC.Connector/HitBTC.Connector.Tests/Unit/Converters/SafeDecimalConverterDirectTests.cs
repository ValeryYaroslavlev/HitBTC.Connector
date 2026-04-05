// Unit/Converters/SafeDecimalConverterDirectTests.cs
using System.Text;
using System.Text.Json;

using FluentAssertions;

using HitBTC.Connector.Core.Converters;

using Xunit;

namespace HitBTC.Connector.Tests.Unit.Converters;

public class SafeDecimalConverterDirectTests
{
    [Theory]
    [InlineData("123.456", 123.456)]
    [InlineData("0", 0)]
    [InlineData("0.00001", 0.00001)]
    [InlineData("-50.5", -50.5)]
    [InlineData("1E-05", 0.00001)]
    [InlineData("1e5", 100000)]
    public void ReadDecimal_ValidStrings_ShouldParse(string input, decimal expected)
    {
        // Arrange
        var json = $"\"{input}\"";
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read(); // Move to first token

        // Act
        var result = SafeDecimalConverter.ReadDecimal(ref reader);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    public void ReadDecimal_InvalidStrings_ShouldReturnNull(string input)
    {
        // Arrange
        var json = $"\"{input}\"";
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();

        // Act
        var result = SafeDecimalConverter.ReadDecimal(ref reader);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ReadDecimal_Null_ShouldReturnNull()
    {
        // Arrange
        var json = "null";
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();

        // Act
        var result = SafeDecimalConverter.ReadDecimal(ref reader);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(123.456)]
    [InlineData(0)]
    [InlineData(-50.5)]
    public void ReadDecimal_Numbers_ShouldParse(decimal expected)
    {
        // Arrange
        var json = expected.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();

        // Act
        var result = SafeDecimalConverter.ReadDecimal(ref reader);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FullRoundtrip_WithModel_ShouldWork()
    {
        // Arrange
        var original = new TestModel { Price = 123.456m, Quantity = 0.001m };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TestModel>(json);

        // Assert
        deserialized!.Price.Should().Be(123.456m);
        deserialized.Quantity.Should().Be(0.001m);
    }

    private class TestModel
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(SafeDecimalConverter))]
        public decimal Price { get; set; }

        [System.Text.Json.Serialization.JsonConverter(typeof(SafeDecimalConverter))]
        public decimal Quantity { get; set; }
    }
}