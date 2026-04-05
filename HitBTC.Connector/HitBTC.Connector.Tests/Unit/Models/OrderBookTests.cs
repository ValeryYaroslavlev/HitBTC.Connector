// Unit/Models/OrderBookTests.cs
using FluentAssertions;

using HitBTC.Connector.Core.Models;

using Xunit;

namespace HitBTC.Connector.Tests.Unit.Models;

public class OrderBookTests
{
    [Fact]
    public void OrderBook_Create_ShouldWork()
    {
        // Act
        using var ob = new OrderBook();

        // Assert
        ob.AskCount.Should().Be(0);
        ob.BidCount.Should().Be(0);
    }

    [Fact]
    public void OrderBook_SetAsks_ShouldWork()
    {
        // Arrange
        using var ob = new OrderBook();
        Span<OrderBookLevel> asks = stackalloc OrderBookLevel[]
        {
            new(50001m, 1.0m),
            new(50002m, 2.0m),
            new(50003m, 0.5m)
        };

        // Act
        ob.SetAsks(asks);

        // Assert
        ob.AskCount.Should().Be(3);
        ob.Asks[0].Price.Should().Be(50001m);
        ob.Asks[0].Quantity.Should().Be(1.0m);
        ob.Asks[2].Price.Should().Be(50003m);
    }

    [Fact]
    public void OrderBook_SetBids_ShouldWork()
    {
        // Arrange
        using var ob = new OrderBook();
        Span<OrderBookLevel> bids = stackalloc OrderBookLevel[]
        {
            new(50000m, 1.5m),
            new(49999m, 2.5m)
        };

        // Act
        ob.SetBids(bids);

        // Assert
        ob.BidCount.Should().Be(2);
        ob.Bids[0].Price.Should().Be(50000m);
        ob.Bids[1].Quantity.Should().Be(2.5m);
    }

    [Fact]
    public void OrderBook_FromSnapshot_ShouldWork()
    {
        // Arrange
        var snapshot = new OrderBookSnapshot
        {
            Timestamp = "2024-01-15T10:30:00.000Z",
            Ask = new[]
            {
            new[] { "50001.50", "1.5" },
            new[] { "50002.00", "2.0" }
        },
            Bid = new[]
            {
            new[] { "50000.00", "1.0" },
            new[] { "49999.50", "2.5" }
        }
        };

        // Act
        using var ob = OrderBook.FromSnapshot(snapshot);

        // Assert
        // Используем DateTimeOffset для корректного сравнения UTC времени
        var expectedTime = DateTimeOffset.Parse(
            "2024-01-15T10:30:00.000Z",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind);

        ob.Timestamp.ToUniversalTime().Should().BeCloseTo(
            expectedTime.UtcDateTime,
            TimeSpan.FromSeconds(1));

        ob.AskCount.Should().Be(2);
        ob.Asks[0].Price.Should().Be(50001.50m);
        ob.Asks[0].Quantity.Should().Be(1.5m);

        ob.BidCount.Should().Be(2);
        ob.Bids[0].Price.Should().Be(50000m);
        ob.Bids[1].Quantity.Should().Be(2.5m);
    }

    [Fact]
    public void OrderBook_Update_ShouldReplaceData()
    {
        // Arrange
        using var ob = new OrderBook();
        Span<OrderBookLevel> initialAsks = stackalloc OrderBookLevel[]
        {
            new(50001m, 1.0m)
        };
        ob.SetAsks(initialAsks);

        // Act
        Span<OrderBookLevel> newAsks = stackalloc OrderBookLevel[]
        {
            new(50002m, 2.0m),
            new(50003m, 3.0m)
        };
        ob.SetAsks(newAsks);

        // Assert
        ob.AskCount.Should().Be(2);
        ob.Asks[0].Price.Should().Be(50002m);
    }

    [Fact]
    public void OrderBook_LargeData_ShouldAutoExpand()
    {
        // Arrange
        using var ob = new OrderBook(10); // Small initial capacity
        var levels = Enumerable.Range(0, 500)
            .Select(i => new OrderBookLevel(50000m + i, 1.0m))
            .ToArray();

        // Act
        ob.SetAsks(levels);

        // Assert
        ob.AskCount.Should().Be(500);
    }

    [Fact]
    public void OrderBook_Dispose_ShouldNotThrow()
    {
        // Arrange
        var ob = new OrderBook();
        ob.SetAsks(stackalloc OrderBookLevel[] { new(50000m, 1m) });

        // Act & Assert
        var action = () => ob.Dispose();
        action.Should().NotThrow();

        // Double dispose should also be safe
        action.Should().NotThrow();
    }

    [Fact]
    public void OrderBookLevel_Equality_ShouldWork()
    {
        // Arrange
        var level1 = new OrderBookLevel(50000m, 1.0m);
        var level2 = new OrderBookLevel(50000m, 1.0m);
        var level3 = new OrderBookLevel(50001m, 1.0m);

        // Assert
        level1.Should().Be(level2);
        level1.Should().NotBe(level3);
    }

    [Fact]
    public void OrderBook_BestBidAsk_Calculation()
    {
        // Arrange
        var snapshot = new OrderBookSnapshot
        {
            Timestamp = "2024-01-15T10:30:00.000Z",
            Ask = new[]
            {
                new[] { "50010.00", "1.0" },
                new[] { "50020.00", "2.0" }
            },
            Bid = new[]
            {
                new[] { "50000.00", "1.0" },
                new[] { "49990.00", "2.0" }
            }
        };

        // Act
        using var ob = OrderBook.FromSnapshot(snapshot);

        // Assert
        var bestAsk = ob.Asks[0].Price;
        var bestBid = ob.Bids[0].Price;
        var spread = bestAsk - bestBid;

        bestAsk.Should().Be(50010m);
        bestBid.Should().Be(50000m);
        spread.Should().Be(10m);
    }
}