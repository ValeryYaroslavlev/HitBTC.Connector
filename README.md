# HitBTC.Connector

.NET
License

## High-performance, lock-free C# connector for HitBTC cryptocurrency exchange API v3.

### ✨ Features
### 🚀 High Performance — Lock-free data structures, zero-allocation hot paths, ArrayPool usage
### 🔒 Thread-Safe — MPSC queues, concurrent collections, safe for multi-threaded trading
### 📡 REST API — Full support for public, spot, and futures endpoints
### 🔌 WebSocket — Real-time market data and order updates
### 🔐 Authentication — HS256 HMAC signature with automatic request signing
### 💪 Robust — Safe decimal parsing, comprehensive error handling
### 🧪 Tested — Unit, integration, and stress tests included


## 📦 Installation

```Bash

dotnet add package HitBTC.Connector

```

####  Or add to your .csproj:

```XML

<PackageReference Include="HitBTC.Connector" Version="1.0.0" />

```

## 🚀 Quick Start
### REST Client — Public Data

```csharp

using HitBTC.Connector.Rest;
using HitBTC.Connector.Core.Models;

// Public API (no authentication required)
await using var client = new HitBtcRestClient("", "");

// Get symbols
var symbols = await client.GetSymbolsAsync();
var btcUsdt = symbols["BTCUSDT"];
Console.WriteLine($"BTCUSDT tick size: {btcUsdt.TickSize}");

// Get order book
using var orderBook = await client.GetOrderBookAsync("BTCUSDT", depth: 10);
Console.WriteLine($"Best bid: {orderBook.Bids[0].Price}, Best ask: {orderBook.Asks[0].Price}");
Console.WriteLine($"Spread: {orderBook.Asks[0].Price - orderBook.Bids[0].Price}");

// Get candles
var candles = await client.GetCandlesAsync("BTCUSDT", CandlePeriod.H1, limit: 24);
foreach (var c in candles)
{
    Console.WriteLine($"{c.Timestamp:HH:mm} O:{c.Open} H:{c.High} L:{c.Low} C:{c.Close}");
}
```

## REST Client — Spot Trading

```csharp

await using var client = new HitBtcRestClient("YOUR_API_KEY", "YOUR_SECRET_KEY");

// Get active orders
var orders = await client.GetActiveSpotOrdersAsync();

// Place limit order
var order = await client.CreateSpotOrderAsync(new CreateSpotOrderRequest
{
    Symbol = "BTCUSDT",
    Side = OrderSide.Buy,
    Quantity = 0.001m,
    Price = 50000m,
    Type = OrderType.Limit,
    TimeInForce = TimeInForce.GTC,
    PostOnly = true
});
Console.WriteLine($"Order placed: {order.ClientOrderId}");

// Replace order
var replaced = await client.ReplaceSpotOrderAsync(order.ClientOrderId, new ReplaceSpotOrderRequest
{
    Quantity = 0.002m,
    Price = 49000m
});

// Cancel order
var canceled = await client.CancelSpotOrderAsync(replaced.ClientOrderId);

// Cancel all orders
await client.CancelAllSpotOrdersAsync(symbol: "BTCUSDT");
```
## REST Client — Futures Trading
```csharp

await using var client = new HitBtcRestClient("YOUR_API_KEY", "YOUR_SECRET_KEY");

// Get futures balance
var balances = await client.GetFuturesBalanceAsync();
foreach (var b in balances)
{
    Console.WriteLine($"{b.Currency}: {b.Available} available");
}

// Create/update margin account
var account = await client.CreateOrUpdateMarginAccountAsync(
    MarginMode.Isolated,
    "BTCUSDT_PERP",
    marginBalance: 100m,
    leverage: 25m
);

// Place futures order
var futuresOrder = await client.CreateFuturesOrderAsync(new CreateFuturesOrderRequest
{
    Symbol = "BTCUSDT_PERP",
    Side = OrderSide.Buy,
    Quantity = 0.01m,
    Price = 50000m,
    Type = OrderType.Limit,
    MarginMode = MarginMode.Isolated,
    ReduceOnly = false
});

// Close position
await client.CloseFuturesPositionAsync(
    MarginMode.Isolated,
    "BTCUSDT_PERP",
    new ClosePositionRequest { Price = 51000m }
);

// Close margin account (withdraw all funds)
await client.CloseMarginAccountAsync(MarginMode.Isolated, "BTCUSDT_PERP");
```


## 📋 API Coverage

### REST API
Category|	Endpoints|	Status|
|-------|------------|--------|
Public|	Symbols, OrderBook, Candles, Trades	|✅|
Spot Trading|	Get/Create/Replace/Cancel Orders|	✅|
Futures Trading|	Orders, Positions, Margin Accounts|	✅|
Futures Balance|	Get Balance by Currency	|✅|
Margin Management|	Create/Update/Close Accounts, Leverage|	✅|


## ⚡ Performance Features
### Lock-Free Object Pool — Reuse objects without locks
### MPSC Queue — Multi-producer single-consumer for WebSocket messages
### ValueStringBuilder — Stack-allocated string building
### ArrayPool — Rent/return buffers for OrderBook levels
### Span/Memory — Zero-copy data access where possible
### SocketsHttpHandler — HTTP/2, connection pooling
