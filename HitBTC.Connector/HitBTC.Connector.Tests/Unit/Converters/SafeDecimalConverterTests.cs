// Unit/Converters/SafeDecimalConverterTests.cs
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;

using HitBTC.Connector.Core.Converters;

using Xunit;

namespace HitBTC.Connector.Tests.Unit.Converters;

public class SafeDecimalConverterTests
{
    // Опции БЕЗ добавления конвертеров глобально
    // Конвертеры применяются через атрибуты на свойствах
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Модель с атрибутом конвертера
    private class TestModel
    {
        [JsonConverter(typeof(SafeDecimalConverter))]
        public decimal Value { get; set; }
    }

    private class TestNullableModel
    {
        [JsonConverter(typeof(SafeNullableDecimalConverter))]
        public decimal? Value { get; set; }
    }

    // ============ SafeDecimalConverter (non-nullable) ============

    [Fact]
    public void Deserialize_NumberAsString_ShouldWork()
    {
        var json = """{"Value": "123.456"}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(123.456m);
    }

    [Fact]
    public void Deserialize_NumberAsNumber_ShouldWork()
    {
        var json = """{"Value": 123.456}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(123.456m);
    }

    [Fact]
    public void Deserialize_EmptyString_ShouldReturnZero()
    {
        var json = """{"Value": ""}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(0m);
    }

    [Fact]
    public void Deserialize_Null_ForNonNullable_ShouldReturnZero()
    {
        var json = """{"Value": null}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(0m);
    }

    [Fact]
    public void Deserialize_InvalidString_ShouldReturnZero()
    {
        var json = """{"Value": "invalid"}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(0m);
    }

    [Fact]
    public void Deserialize_Infinity_ShouldReturnZero()
    {
        var json = """{"Value": "Infinity"}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(0m);
    }

    [Fact]
    public void Deserialize_NaN_ShouldReturnZero()
    {
        var json = """{"Value": "NaN"}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(0m);
    }

    [Fact]
    public void Deserialize_VerySmallNumber_ShouldWork()
    {
        var json = """{"Value": "0.00000001"}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(0.00000001m);
    }

    [Fact]
    public void Deserialize_VeryLargeNumber_ShouldWork()
    {
        var json = """{"Value": "99999999999999999999"}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(99999999999999999999m);
    }

    [Fact]
    public void Deserialize_NegativeNumber_ShouldWork()
    {
        var json = """{"Value": "-123.45"}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(-123.45m);
    }

    [Fact]
    public void Deserialize_ScientificNotation_ShouldWork()
    {
        var json = """{"Value": "1E-05"}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(0.00001m);
    }

    [Fact]
    public void Deserialize_Zero_ShouldWork()
    {
        var json = """{"Value": "0"}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(0m);
    }

    [Fact]
    public void Deserialize_ZeroAsNumber_ShouldWork()
    {
        var json = """{"Value": 0}""";
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);
        result!.Value.Should().Be(0m);
    }

    // ============ SafeNullableDecimalConverter ============

    [Fact]
    public void Deserialize_Nullable_EmptyString_ShouldReturnNull()
    {
        var json = """{"Value": ""}""";
        var result = JsonSerializer.Deserialize<TestNullableModel>(json, _options);
        result!.Value.Should().BeNull();
    }

    [Fact]
    public void Deserialize_Nullable_Null_ShouldReturnNull()
    {
        var json = """{"Value": null}""";
        var result = JsonSerializer.Deserialize<TestNullableModel>(json, _options);
        result!.Value.Should().BeNull();
    }

    [Fact]
    public void Deserialize_Nullable_ValidNumberAsString_ShouldWork()
    {
        var json = """{"Value": "50.00"}""";
        var result = JsonSerializer.Deserialize<TestNullableModel>(json, _options);
        result!.Value.Should().Be(50m);
    }

    [Fact]
    public void Deserialize_Nullable_ValidNumberAsNumber_ShouldWork()
    {
        var json = """{"Value": 50.00}""";
        var result = JsonSerializer.Deserialize<TestNullableModel>(json, _options);
        result!.Value.Should().Be(50m);
    }

    [Fact]
    public void Deserialize_Nullable_WhitespaceString_ShouldReturnNull()
    {
        var json = """{"Value": "   "}""";
        var result = JsonSerializer.Deserialize<TestNullableModel>(json, _options);
        result!.Value.Should().BeNull();
    }

    [Fact]
    public void Deserialize_Nullable_InvalidString_ShouldReturnNull()
    {
        var json = """{"Value": "invalid"}""";
        var result = JsonSerializer.Deserialize<TestNullableModel>(json, _options);
        result!.Value.Should().BeNull();
    }

    [Fact]
    public void Deserialize_Nullable_Infinity_ShouldReturnNull()
    {
        var json = """{"Value": "Infinity"}""";
        var result = JsonSerializer.Deserialize<TestNullableModel>(json, _options);
        result!.Value.Should().BeNull();
    }

    // ============ Serialization ============

    [Fact]
    public void Serialize_NonNullable_ShouldProduceString()
    {
        var model = new TestModel { Value = 123.456m };
        var json = JsonSerializer.Serialize(model, _options);
        json.Should().Contain("123.456");
    }

    [Fact]
    public void Serialize_Nullable_WithValue_ShouldProduceString()
    {
        var model = new TestNullableModel { Value = 123.456m };
        var json = JsonSerializer.Serialize(model, _options);
        json.Should().Contain("123.456");
    }

    [Fact]
    public void Serialize_Nullable_Null_ShouldProduceNull()
    {
        var model = new TestNullableModel { Value = null };
        var json = JsonSerializer.Serialize(model, _options);
        json.Should().Contain("null");
    }

    // ============ Real-world scenarios ============

    [Fact]
    public void Deserialize_RealWorldLeverageResponse_EmptyString_ShouldWork()
    {
        // Симулируем реальный ответ API для cross margin (leverage = "")
        var json = """
        {
            "symbol": "BTCUSDT_PERP",
            "type": "cross",
            "leverage": "",
            "currencies": []
        }
        """;

        var result = JsonSerializer.Deserialize<FuturesAccountTest>(json, _options);
        result.Should().NotBeNull();
        result!.Symbol.Should().Be("BTCUSDT_PERP");
        result.Type.Should().Be("cross");
        result.Leverage.Should().BeNull();
    }

    [Fact]
    public void Deserialize_RealWorldLeverageResponse_WithValue_ShouldWork()
    {
        // Симулируем реальный ответ API для isolated margin (leverage = "50.00")
        var json = """
        {
            "symbol": "BTCUSDT_PERP",
            "type": "isolated",
            "leverage": "50.00",
            "currencies": []
        }
        """;

        var result = JsonSerializer.Deserialize<FuturesAccountTest>(json, _options);
        result.Should().NotBeNull();
        result!.Symbol.Should().Be("BTCUSDT_PERP");
        result.Type.Should().Be("isolated");
        result.Leverage.Should().Be(50m);
    }

    [Fact]
    public void Deserialize_RealWorldLeverageResponse_WithNull_ShouldWork()
    {
        var json = """
        {
            "symbol": "BTCUSDT_PERP",
            "type": "cross",
            "leverage": null,
            "currencies": []
        }
        """;

        var result = JsonSerializer.Deserialize<FuturesAccountTest>(json, _options);
        result.Should().NotBeNull();
        result!.Leverage.Should().BeNull();
    }

    [Fact]
    public void Deserialize_MissingProperty_ShouldUseDefault()
    {
        var json = """
        {
            "symbol": "BTCUSDT_PERP",
            "type": "isolated"
        }
        """;

        var result = JsonSerializer.Deserialize<FuturesAccountTest>(json, _options);
        result.Should().NotBeNull();
        result!.Leverage.Should().BeNull(); // default for nullable
    }

    [Fact]
    public void Deserialize_ComplexModel_ShouldWork()
    {
        var json = """
        {
            "symbol": "BTCUSDT_PERP",
            "type": "isolated",
            "leverage": "100.00",
            "currencies": [
                {
                    "code": "USDT",
                    "margin_balance": "500.123456",
                    "reserved_orders": "0",
                    "reserved_positions": "100.5"
                }
            ]
        }
        """;

        var result = JsonSerializer.Deserialize<FuturesAccountTestFull>(json, _options);
        result.Should().NotBeNull();
        result!.Leverage.Should().Be(100m);
        result.Currencies.Should().HaveCount(1);
        result.Currencies[0].MarginBalance.Should().Be(500.123456m);
        result.Currencies[0].ReservedOrders.Should().Be(0m);
        result.Currencies[0].ReservedPositions.Should().Be(100.5m);
    }

    // ============ Test Models ============

    private class FuturesAccountTest
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("leverage")]
        [JsonConverter(typeof(SafeNullableDecimalConverter))]
        public decimal? Leverage { get; set; }

        [JsonPropertyName("currencies")]
        public object[] Currencies { get; set; } = Array.Empty<object>();
    }

    private class FuturesAccountTestFull
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("leverage")]
        [JsonConverter(typeof(SafeNullableDecimalConverter))]
        public decimal? Leverage { get; set; }

        [JsonPropertyName("currencies")]
        public CurrencyTest[] Currencies { get; set; } = Array.Empty<CurrencyTest>();
    }

    private class CurrencyTest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("margin_balance")]
        [JsonConverter(typeof(SafeDecimalConverter))]
        public decimal MarginBalance { get; set; }

        [JsonPropertyName("reserved_orders")]
        [JsonConverter(typeof(SafeDecimalConverter))]
        public decimal ReservedOrders { get; set; }

        [JsonPropertyName("reserved_positions")]
        [JsonConverter(typeof(SafeDecimalConverter))]
        public decimal ReservedPositions { get; set; }
    }
}