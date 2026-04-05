// Integration/PublicApiTests.cs
using FluentAssertions;

using HitBTC.Connector.Core.Models;
using HitBTC.Connector.Rest;
using HitBTC.Connector.Tests.Fixtures;

using Xunit;

namespace HitBTC.Connector.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Integration")]
public class PublicApiTests : IAsyncLifetime
{
    private HitBtcRestClient _client = null!;

    public Task InitializeAsync()
    {
        // Публичный API не требует ключей
        _client = new HitBtcRestClient(string.Empty, string.Empty);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _client.DisposeAsync();
    }

    [Fact]
    public async Task GetSymbols_ShouldReturnValidData()
    {
        // Skip if integration tests disabled
        Skip.IfNot(TestConfiguration.RunIntegrationTests, "Integration tests disabled");

        // Act
        var symbols = await _client.GetSymbolsAsync();

        // Assert
        symbols.Should().NotBeEmpty();
        symbols.Should().ContainKey("BTCUSDT");

        var btcUsdt = symbols["BTCUSDT"];
        btcUsdt.Type.Should().Be("spot");
        btcUsdt.BaseCurrency.Should().Be("BTC");
        btcUsdt.QuoteCurrency.Should().Be("USDT");
        btcUsdt.Status.Should().Be("working");
        btcUsdt.QuantityIncrement.Should().BeGreaterThan(0);
        btcUsdt.TickSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSymbol_Single_ShouldWork()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests, "Integration tests disabled");

        // Act
        var symbol = await _client.GetSymbolAsync(TestConfiguration.TestSymbol);

        // Assert
        symbol.Should().NotBeNull();
        symbol!.Status.Should().Be("working");
    }

    [Fact]
    public async Task GetSymbols_WithFilter_ShouldReturnFiltered()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests, "Integration tests disabled");

        // Act
        var symbols = await _client.GetSymbolsAsync(new[] { "BTCUSDT", "ETHBTC" });

        // Assert
        symbols.Should().HaveCount(2);
        symbols.Keys.Should().Contain("BTCUSDT");
        symbols.Keys.Should().Contain("ETHBTC");
    }

    [Fact]
    public async Task GetOrderBook_ShouldReturnValidOrderBook()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests, "Integration tests disabled");

        // Act
        using var orderBook = await _client.GetOrderBookAsync(TestConfiguration.TestSymbol, depth: 10);

        // Assert
        orderBook.Should().NotBeNull();
        orderBook.AskCount.Should().BeGreaterThan(0);
        orderBook.BidCount.Should().BeGreaterThan(0);

        // Best ask should be higher than best bid
        orderBook.Asks[0].Price.Should().BeGreaterThan(orderBook.Bids[0].Price);

        // Asks should be in ascending order
        for (int i = 1; i < orderBook.AskCount; i++)
        {
            orderBook.Asks[i].Price.Should().BeGreaterThanOrEqualTo(orderBook.Asks[i - 1].Price);
        }

        // Bids should be in descending order
        for (int i = 1; i < orderBook.BidCount; i++)
        {
            orderBook.Bids[i].Price.Should().BeLessThanOrEqualTo(orderBook.Bids[i - 1].Price);
        }
    }

    [Fact]
    public async Task GetCandles_ShouldReturnValidCandles()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests, "Integration tests disabled");

        // Act
        var candles = await _client.GetCandlesAsync(
            TestConfiguration.TestSymbol,
            CandlePeriod.H1,
            limit: 24);

        // Assert
        candles.Should().NotBeEmpty();
        candles.Should().HaveCountLessOrEqualTo(24);

        foreach (var candle in candles)
        {
            candle.Open.Should().BeGreaterThan(0);
            candle.Close.Should().BeGreaterThan(0);
            candle.High.Should().BeGreaterThanOrEqualTo(candle.Open);
            candle.High.Should().BeGreaterThanOrEqualTo(candle.Close);
            candle.Low.Should().BeLessThanOrEqualTo(candle.Open);
            candle.Low.Should().BeLessThanOrEqualTo(candle.Close);
            candle.Volume.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task GetCandles_WithDateRange_ShouldWork()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests, "Integration tests disabled");

        // Arrange
        var till = DateTime.UtcNow;
        var from = till.AddDays(-7);

        // Act
        var candles = await _client.GetCandlesAsync(
            TestConfiguration.TestSymbol,
            CandlePeriod.D1,
            from: from,
            till: till);

        // Assert
        candles.Should().NotBeEmpty();
        foreach (var candle in candles)
        {
            candle.Timestamp.Should().BeOnOrAfter(from);
            candle.Timestamp.Should().BeOnOrBefore(till);
        }
    }
}