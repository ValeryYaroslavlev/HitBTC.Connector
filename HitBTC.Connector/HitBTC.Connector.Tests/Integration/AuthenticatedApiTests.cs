// Integration/AuthenticatedApiTests.cs
using FluentAssertions;

using HitBTC.Connector.Core.Models;
using HitBTC.Connector.Rest;
using HitBTC.Connector.Tests.Fixtures;

using Xunit;

namespace HitBTC.Connector.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Integration")]
public class AuthenticatedApiTests : IAsyncLifetime
{
    private HitBtcRestClient _client = null!;

    public Task InitializeAsync()
    {
        if (!TestConfiguration.HasCredentials)
        {
            return Task.CompletedTask;
        }

        _client = new HitBtcRestClient(
            TestConfiguration.ApiKey,
            TestConfiguration.SecretKey);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetActiveSpotOrders_ShouldWork()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests && TestConfiguration.HasCredentials,
            "Integration tests disabled or no credentials");

        // Act
        var orders = await _client.GetActiveSpotOrdersAsync();

        // Assert
        orders.Should().NotBeNull();
        // Может быть пустым если нет активных ордеров
    }

    [Fact]
    public async Task GetFuturesBalance_ShouldWork()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests && TestConfiguration.HasCredentials,
            "Integration tests disabled or no credentials");

        // Act
        var balances = await _client.GetFuturesBalanceAsync();

        // Assert
        balances.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFuturesAccounts_ShouldWork()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests && TestConfiguration.HasCredentials,
            "Integration tests disabled or no credentials");

        // Act
        var accounts = await _client.GetFuturesAccountsAsync();

        // Assert
        accounts.Should().NotBeNull();
    }

    [Fact]
    public async Task SpotOrder_CreateAndCancel_Flow()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests && TestConfiguration.HasCredentials,
            "Integration tests disabled or no credentials");

        // Arrange - создаём ордер с очень низкой ценой чтобы он не исполнился
        var symbol = await _client.GetSymbolAsync(TestConfiguration.TestSymbol);
        using var orderBook = await _client.GetOrderBookAsync(TestConfiguration.TestSymbol, 1);

        // Ставим цену на 50% ниже текущей bid
        var safePrice = Math.Round(orderBook.Bids[0].Price * 0.5m, 2);
        var minQuantity = symbol!.QuantityIncrement;

        var createRequest = new CreateSpotOrderRequest
        {
            Symbol = TestConfiguration.TestSymbol,
            Side = OrderSide.Buy,
            Quantity = minQuantity,
            Price = safePrice,
            Type = OrderType.Limit,
            TimeInForce = TimeInForce.GTC
        };

        // Act - Create
        SpotOrder? createdOrder = null;
        try
        {
            createdOrder = await _client.CreateSpotOrderAsync(createRequest);

            // Assert - Created
            createdOrder.Should().NotBeNull();
            createdOrder.Status.Should().Be(OrderStatus.New);
            createdOrder.Symbol.Should().Be(TestConfiguration.TestSymbol);
            createdOrder.Price.Should().Be(safePrice);

            // Act - Get
            var fetchedOrder = await _client.GetSpotOrderAsync(createdOrder.ClientOrderId);
            fetchedOrder.Should().NotBeNull();
            fetchedOrder.Id.Should().Be(createdOrder.Id);
        }
        finally
        {
            // Cleanup - Cancel
            if (createdOrder is not null)
            {
                var canceledOrder = await _client.CancelSpotOrderAsync(createdOrder.ClientOrderId);
                canceledOrder.Status.Should().Be(OrderStatus.Canceled);
            }
        }
    }

    [Fact]
    public async Task SpotOrder_ReplaceOrder_Flow()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests && TestConfiguration.HasCredentials,
            "Integration tests disabled or no credentials");

        // Arrange
        var symbol = await _client.GetSymbolAsync(TestConfiguration.TestSymbol);
        using var orderBook = await _client.GetOrderBookAsync(TestConfiguration.TestSymbol, 1);

        var safePrice = Math.Round(orderBook.Bids[0].Price * 0.5m, 2);
        var minQuantity = symbol!.QuantityIncrement;

        SpotOrder? order = null;
        try
        {
            // Create
            order = await _client.CreateSpotOrderAsync(new CreateSpotOrderRequest
            {
                Symbol = TestConfiguration.TestSymbol,
                Side = OrderSide.Buy,
                Quantity = minQuantity,
                Price = safePrice
            });

            // Replace
            var newPrice = Math.Round(safePrice * 0.9m, 2);
            var newQuantity = minQuantity * 2;

            var replacedOrder = await _client.ReplaceSpotOrderAsync(
                order.ClientOrderId,
                new ReplaceSpotOrderRequest
                {
                    Quantity = newQuantity,
                    Price = newPrice
                });

            // Assert
            replacedOrder.Price.Should().Be(newPrice);
            replacedOrder.Quantity.Should().Be(newQuantity);

            order = replacedOrder; // Update reference for cleanup
        }
        finally
        {
            if (order is not null)
            {
                await _client.CancelSpotOrderAsync(order.ClientOrderId);
            }
        }
    }

    [Fact]
    public async Task CancelAllSpotOrders_ShouldWork()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests && TestConfiguration.HasCredentials,
            "Integration tests disabled or no credentials");

        // Arrange - create a few orders
        var symbol = await _client.GetSymbolAsync(TestConfiguration.TestSymbol);
        using var orderBook = await _client.GetOrderBookAsync(TestConfiguration.TestSymbol, 1);

        var safePrice = Math.Round(orderBook.Bids[0].Price * 0.5m, 2);
        var orders = new List<SpotOrder>();

        for (int i = 0; i < 3; i++)
        {
            var order = await _client.CreateSpotOrderAsync(new CreateSpotOrderRequest
            {
                Symbol = TestConfiguration.TestSymbol,
                Side = OrderSide.Buy,
                Quantity = symbol!.QuantityIncrement,
                Price = safePrice - i * symbol.TickSize * 10
            });
            orders.Add(order);
        }

        // Act
        var canceledOrders = await _client.CancelAllSpotOrdersAsync(TestConfiguration.TestSymbol);

        // Assert
        canceledOrders.Should().HaveCountGreaterOrEqualTo(3);
        canceledOrders.Should().OnlyContain(o => o.Status == OrderStatus.Canceled);
    }
}