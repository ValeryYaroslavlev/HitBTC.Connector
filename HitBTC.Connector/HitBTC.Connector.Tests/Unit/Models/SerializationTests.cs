// Unit/Models/SerializationTests.cs
using System.Text.Json;

using FluentAssertions;

using HitBTC.Connector.Core.Models;
using HitBTC.Connector.Tests.Fixtures;

using Xunit;

namespace HitBTC.Connector.Tests.Unit.Models;

public class SerializationTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    [Fact]
    public void SymbolInfo_Deserialize_ShouldWork()
    {
        // Arrange
        var json = TestData.Json.SymbolResponse;

        // Act
        var result = JsonSerializer.Deserialize<Dictionary<string, SymbolInfo>>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("BTCUSDT");

        var symbol = result!["BTCUSDT"];
        symbol.Type.Should().Be("spot");
        symbol.BaseCurrency.Should().Be("BTC");
        symbol.QuoteCurrency.Should().Be("USDT");
        symbol.Status.Should().Be("working");
        symbol.QuantityIncrement.Should().Be(0.00001m);
        symbol.TickSize.Should().Be(0.01m);
        symbol.TakeRate.Should().Be(0.0025m);
        symbol.MakeRate.Should().Be(0.001m);
        symbol.FeeCurrency.Should().Be("USDT");
        symbol.MarginTrading.Should().BeTrue();
        symbol.MaxInitialLeverage.Should().Be(10m);
    }

    [Fact]
    public void SpotOrder_Deserialize_ShouldWork()
    {
        // Arrange
        var json = TestData.Json.SpotOrderResponse;

        // Act
        var order = JsonSerializer.Deserialize<SpotOrder>(json, _options);

        // Assert
        order.Should().NotBeNull();
        order!.Id.Should().Be(12345678);
        order.ClientOrderId.Should().Be("test_order_001");
        order.Symbol.Should().Be("BTCUSDT");
        order.Side.Should().Be(OrderSide.Buy);
        order.Status.Should().Be(OrderStatus.New);
        order.Type.Should().Be(OrderType.Limit);
        order.TimeInForce.Should().Be(TimeInForce.GTC);
        order.Quantity.Should().Be(0.001m);
        order.Price.Should().Be(50000m);
        order.QuantityCumulative.Should().Be(0m);
        order.PostOnly.Should().BeTrue();
    }

    [Fact]
    public void SpotOrderArray_Deserialize_ShouldWork()
    {
        // Arrange
        var json = TestData.Json.SpotOrdersArrayResponse;

        // Act
        var orders = JsonSerializer.Deserialize<SpotOrder[]>(json, _options);

        // Assert
        orders.Should().NotBeNull();
        orders.Should().HaveCount(1);
        orders![0].ClientOrderId.Should().Be("test_order_001");
    }

    [Fact]
    public void Candle_Deserialize_ShouldWork()
    {
        // Arrange
        var json = TestData.Json.CandlesResponse;

        // Act
        var candles = JsonSerializer.Deserialize<Candle[]>(json, _options);

        // Assert
        candles.Should().NotBeNull();
        candles.Should().HaveCount(2);

        var candle = candles![0];
        candle.Open.Should().Be(50000m);
        candle.Close.Should().Be(50100m);
        candle.Low.Should().Be(49950m);
        candle.High.Should().Be(50150m);
        candle.Volume.Should().Be(100.5m);
        candle.VolumeQuote.Should().Be(5025000m);
    }

    [Fact]
    public void FuturesBalance_Deserialize_ShouldWork()
    {
        // Arrange
        var json = TestData.Json.FuturesBalanceResponse;

        // Act
        var balances = JsonSerializer.Deserialize<FuturesBalance[]>(json, _options);

        // Assert
        balances.Should().NotBeNull();
        balances.Should().HaveCount(1);

        var balance = balances![0];
        balance.Currency.Should().Be("USDT");
        balance.Available.Should().Be(1000.50m);
        balance.Reserved.Should().Be(100m);
        balance.ReservedMargin.Should().Be(50m);
        balance.CrossMarginReserved.Should().Be(0m);
    }

    [Fact]
    public void FuturesMarginAccount_Deserialize_ShouldWork()
    {
        // Arrange
        var json = TestData.Json.FuturesAccountResponse;

        // Act
        var account = JsonSerializer.Deserialize<FuturesMarginAccount>(json, _options);

        // Assert
        account.Should().NotBeNull();
        account!.Symbol.Should().Be("BTCUSDT_PERP");
        account.Type.Should().Be("isolated");
        account.Leverage.Should().Be(50m);
        account.MarginCall.Should().BeFalse();

        account.Currencies.Should().HaveCount(1);
        account.Currencies[0].Code.Should().Be("USDT");
        account.Currencies[0].MarginBalance.Should().Be(500m);

        account.Positions.Should().HaveCount(1);
        var position = account.Positions![0];
        position.Id.Should().Be(123456);
        position.Quantity.Should().Be(0.01m);
        position.PriceEntry.Should().Be(50000m);
        position.Pnl.Should().Be(50m);
    }

    [Fact]
    public void OrderSide_Deserialize_Buy_ShouldWork()
    {
        // Arrange
        var json = """{"side": "buy"}""";

        // Act
        var obj = JsonSerializer.Deserialize<TestSideWrapper>(json, _options);

        // Assert
        obj!.Side.Should().Be(OrderSide.Buy);
    }

    [Fact]
    public void OrderSide_Deserialize_Sell_ShouldWork()
    {
        // Arrange
        var json = """{"side": "sell"}""";

        // Act
        var obj = JsonSerializer.Deserialize<TestSideWrapper>(json, _options);

        // Assert
        obj!.Side.Should().Be(OrderSide.Sell);
    }

    [Fact]
    public void OrderStatus_Deserialize_AllValues_ShouldWork()
    {
        // Arrange & Act & Assert
        var statuses = new[]
        {
            ("new", OrderStatus.New),
            ("suspended", OrderStatus.Suspended),
            ("partiallyFilled", OrderStatus.PartiallyFilled),
            ("filled", OrderStatus.Filled),
            ("canceled", OrderStatus.Canceled),
            ("expired", OrderStatus.Expired)
        };

        foreach (var (jsonValue, expectedEnum) in statuses)
        {
            var json = $$"""{"status": "{{jsonValue}}"}""";
            var obj = JsonSerializer.Deserialize<TestStatusWrapper>(json, _options);
            obj!.Status.Should().Be(expectedEnum, $"because '{jsonValue}' should deserialize to {expectedEnum}");
        }
    }

    [Fact]
    public void OrderType_Deserialize_AllValues_ShouldWork()
    {
        var types = new[]
        {
            ("limit", OrderType.Limit),
            ("market", OrderType.Market),
            ("stopLimit", OrderType.StopLimit),
            ("stopMarket", OrderType.StopMarket),
            ("takeProfitLimit", OrderType.TakeProfitLimit),
            ("takeProfitMarket", OrderType.TakeProfitMarket)
        };

        foreach (var (jsonValue, expectedEnum) in types)
        {
            var json = $$"""{"type": "{{jsonValue}}"}""";
            var obj = JsonSerializer.Deserialize<TestTypeWrapper>(json, _options);
            obj!.Type.Should().Be(expectedEnum);
        }
    }

    [Fact]
    public void NullableDecimal_Deserialize_ShouldHandleNull()
    {
        // Arrange
        var json = """{"price": null}""";

        // Act
        var obj = JsonSerializer.Deserialize<TestPriceWrapper>(json, _options);

        // Assert
        obj!.Price.Should().BeNull();
    }

    [Fact]
    public void Decimal_Deserialize_FromString_ShouldWork()
    {
        // Arrange
        var json = """{"price": "50000.123456789"}""";

        // Act
        var obj = JsonSerializer.Deserialize<TestPriceWrapper>(json, _options);

        // Assert
        obj!.Price.Should().Be(50000.123456789m);
    }

    // Helper classes for testing
    private class TestSideWrapper
    {
        public OrderSide Side { get; set; }
    }

    private class TestStatusWrapper
    {
        public OrderStatus Status { get; set; }
    }

    private class TestTypeWrapper
    {
        public OrderType Type { get; set; }
    }

    private class TestPriceWrapper
    {
        [System.Text.Json.Serialization.JsonNumberHandling(
            System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
        public decimal? Price { get; set; }
    }
}