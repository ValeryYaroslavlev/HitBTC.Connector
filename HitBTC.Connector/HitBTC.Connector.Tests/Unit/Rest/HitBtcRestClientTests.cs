// Unit/Rest/HitBtcRestClientTests.cs
using System.Net;

using FluentAssertions;

using HitBTC.Connector.Core.Models;
using HitBTC.Connector.Rest;
using HitBTC.Connector.Tests.Fixtures;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

using Xunit;

namespace HitBTC.Connector.Tests.Unit.Rest;

public class HitBtcRestClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly HitBtcRestClient _client;

    public HitBtcRestClientTests()
    {
        _server = WireMockServer.Start();
        _client = CreateClientWithMockServer(_server.Urls[0]);
    }

    private static HitBtcRestClient CreateClientWithMockServer(string baseUrl)
    {
        var client = new HitBtcRestClient("testApiKey", "testSecretKey");

        var publicClientField = typeof(HitBtcRestClient)
            .GetField("_publicClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var authClientField = typeof(HitBtcRestClient)
            .GetField("_authClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var publicClient = (HttpClient)publicClientField!.GetValue(client)!;
        var authClient = (HttpClient)authClientField!.GetValue(client)!;

        // Важно: URL должен заканчиваться на /
        publicClient.BaseAddress = new Uri(baseUrl + "/api/3/");
        authClient.BaseAddress = new Uri(baseUrl + "/api/3/");

        return client;
    }

    [Fact]
    public async Task GetSymbolsAsync_ShouldReturnSymbols()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/3/public/symbol")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(TestData.Json.SymbolResponse));

        // Act
        var symbols = await _client.GetSymbolsAsync();

        // Assert
        symbols.Should().NotBeNull();
        symbols.Should().ContainKey("BTCUSDT");
        symbols["BTCUSDT"].QuantityIncrement.Should().Be(0.00001m);
    }

    [Fact]
    public async Task GetSymbolsAsync_WithFilter_ShouldIncludeQueryParam()
    {
        // Arrange - матчим любой запрос к /public/symbol
        _server
            .Given(Request.Create()
                .WithPath("/api/3/public/symbol")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(TestData.Json.SymbolResponse));

        // Act
        var symbols = await _client.GetSymbolsAsync(new[] { "BTCUSDT", "ETHBTC" });

        // Assert
        symbols.Should().NotBeNull();
        symbols.Should().ContainKey("BTCUSDT");

        // Проверяем, что запрос был с правильными параметрами
        var lastEntry = _server.LogEntries.LastOrDefault();
        lastEntry.Should().NotBeNull();

        var rawQuery = lastEntry!.RequestMessage.RawQuery ?? string.Empty;
        rawQuery.Should().Contain("symbols=");
        rawQuery.Should().Contain("BTCUSDT");
        rawQuery.Should().Contain("ETHBTC");
    }

    [Fact]
    public async Task GetOrderBookAsync_ShouldReturnOrderBook()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/3/public/orderbook/BTCUSDT")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(TestData.Json.OrderBookResponse));

        // Act
        using var orderBook = await _client.GetOrderBookAsync("BTCUSDT");

        // Assert
        orderBook.Should().NotBeNull();
        orderBook.AskCount.Should().Be(3);
        orderBook.BidCount.Should().Be(3);
        orderBook.Asks[0].Price.Should().Be(50001.50m);
        orderBook.Bids[0].Price.Should().Be(50000m);
    }

    [Fact]
    public async Task GetCandlesAsync_ShouldReturnCandles()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/3/public/candles/BTCUSDT")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(TestData.Json.CandlesResponse));

        // Act
        var candles = await _client.GetCandlesAsync("BTCUSDT", CandlePeriod.H1, limit: 2);

        // Assert
        candles.Should().NotBeNull();
        candles.Should().HaveCount(2);
        candles[0].Open.Should().Be(50000m);
        candles[0].High.Should().Be(50150m);
    }

    [Fact]
    public async Task GetActiveSpotOrdersAsync_ShouldReturnOrders()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/3/spot/order")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(TestData.Json.SpotOrdersArrayResponse));

        // Act
        var orders = await _client.GetActiveSpotOrdersAsync();

        // Assert
        orders.Should().NotBeNull();
        orders.Should().HaveCount(1);
        orders[0].Symbol.Should().Be("BTCUSDT");
    }

    [Fact]
    public async Task CreateSpotOrderAsync_ShouldSendCorrectRequest()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/3/spot/order")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(TestData.Json.SpotOrderResponse));

        var request = new CreateSpotOrderRequest
        {
            Symbol = "BTCUSDT",
            Side = OrderSide.Buy,
            Quantity = 0.001m,
            Price = 50000m,
            PostOnly = true
        };

        // Act
        var order = await _client.CreateSpotOrderAsync(request);

        // Assert
        order.Should().NotBeNull();
        order.ClientOrderId.Should().Be("test_order_001");
        order.Status.Should().Be(OrderStatus.New);
    }

    [Fact]
    public async Task CancelSpotOrderAsync_ShouldReturnCanceledOrder()
    {
        // Arrange
        var canceledOrderJson = """
        {
            "id": 12345678,
            "client_order_id": "test_order_001",
            "symbol": "BTCUSDT",
            "side": "buy",
            "status": "canceled",
            "type": "limit",
            "time_in_force": "GTC",
            "quantity": "0.001",
            "price": "50000.00",
            "quantity_cumulative": "0",
            "post_only": false,
            "created_at": "2024-01-15T10:30:00.000Z",
            "updated_at": "2024-01-15T10:35:00.000Z"
        }
        """;

        _server
            .Given(Request.Create()
                .WithPath("/api/3/spot/order/test_order_001")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(canceledOrderJson));

        // Act
        var order = await _client.CancelSpotOrderAsync("test_order_001");

        // Assert
        order.Status.Should().Be(OrderStatus.Canceled);
    }

    [Fact]
    public async Task CreateSpotOrderAsync_InsufficientFunds_ShouldThrow()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/3/spot/order")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody(TestData.Json.ErrorResponse));

        var request = new CreateSpotOrderRequest
        {
            Symbol = "BTCUSDT",
            Side = OrderSide.Buy,
            Quantity = 1000m,
            Price = 50000m
        };

        // Act
        var action = () => _client.CreateSpotOrderAsync(request);

        // Assert
        await action.Should().ThrowAsync<HitBtcApiException>()
            .Where(e => e.StatusCode == 400);
    }

    [Fact]
    public async Task CancelSpotOrderAsync_OrderNotFound_ShouldThrow()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/3/spot/order/nonexistent")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody(TestData.Json.OrderNotFoundError));

        // Act
        var action = () => _client.CancelSpotOrderAsync("nonexistent");

        // Assert
        await action.Should().ThrowAsync<HitBtcApiException>()
            .Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task GetFuturesBalanceAsync_ShouldReturnBalances()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/3/futures/balance")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(TestData.Json.FuturesBalanceResponse));

        // Act
        var balances = await _client.GetFuturesBalanceAsync();

        // Assert
        balances.Should().HaveCount(1);
        balances[0].Currency.Should().Be("USDT");
        balances[0].Available.Should().Be(1000.50m);
    }

    [Fact]
    public async Task CreateOrUpdateMarginAccountAsync_ShouldWork()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/api/3/futures/account/isolated/BTCUSDT_PERP")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(TestData.Json.FuturesAccountResponse));

        // Act
        var account = await _client.CreateOrUpdateMarginAccountAsync(
            MarginMode.Isolated,
            "BTCUSDT_PERP",
            marginBalance: 500m,
            leverage: 50m);

        // Assert
        account.Should().NotBeNull();
        account.Symbol.Should().Be("BTCUSDT_PERP");
        account.Leverage.Should().Be(50m);
    }

    [Fact]
    public async Task CreateOrUpdateMarginAccountAsync_CrossWithLeverage_ShouldThrow()
    {
        // Act
        var action = () => _client.CreateOrUpdateMarginAccountAsync(
            MarginMode.Cross,
            "BTCUSDT_PERP",
            marginBalance: 500m,
            leverage: 50m);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Leverage cannot be specified for cross margin*");
    }

    [Fact]
    public async Task CreateOrUpdateMarginAccountAsync_InvalidLeverage_ShouldThrow()
    {
        // Act - leverage слишком большой
        var action = () => _client.CreateOrUpdateMarginAccountAsync(
            MarginMode.Isolated,
            "BTCUSDT_PERP",
            marginBalance: 500m,
            leverage: 1001m);

        // Assert
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ReplaceSpotOrderAsync_ShouldWork()
    {
        // Arrange
        var replacedOrderJson = """
        {
            "id": 12345679,
            "client_order_id": "test_order_002",
            "symbol": "BTCUSDT",
            "side": "buy",
            "status": "new",
            "type": "limit",
            "time_in_force": "GTC",
            "quantity": "0.002",
            "price": "51000.00",
            "quantity_cumulative": "0",
            "post_only": false,
            "created_at": "2024-01-15T10:30:00.000Z",
            "updated_at": "2024-01-15T10:35:00.000Z"
        }
        """;

        _server
            .Given(Request.Create()
                .WithPath("/api/3/spot/order/test_order_001")
                .UsingMethod("PATCH"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(replacedOrderJson));

        var request = new ReplaceSpotOrderRequest
        {
            Quantity = 0.002m,
            Price = 51000m,
            NewClientOrderId = "test_order_002"
        };

        // Act
        var order = await _client.ReplaceSpotOrderAsync("test_order_001", request);

        // Assert
        order.ClientOrderId.Should().Be("test_order_002");
        order.Quantity.Should().Be(0.002m);
        order.Price.Should().Be(51000m);
    }

    [Fact]
    public async Task NetworkError_ShouldThrowHttpRequestException()
    {
        // Arrange - сервер не отвечает
        _server.Stop();

        // Act
        var action = () => _client.GetSymbolsAsync();

        // Assert
        await action.Should().ThrowAsync<HttpRequestException>();
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
        _client.DisposeAsync().AsTask().Wait();
    }
}