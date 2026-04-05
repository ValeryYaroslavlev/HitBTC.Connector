// Program.cs
using HitBTC.Connector.Core.Models;
using HitBTC.Connector.Rest;
using HitBTC.Connector.Trading;

// ============ ПРИМЕР 1: Только REST ============

await using var restClient = new HitBtcRestClient("api-key", "token");

// Публичные данные
var symbols = await restClient.GetSymbolsAsync(new[] { "BTCUSDT", "ETHBTC" });
foreach (var (code, info) in symbols)
{
    Console.WriteLine($"{code}: {info.Status}, tick={info.TickSize}");
}

var ethSymbol = await restClient.GetSymbolAsync("ETHBTC");
Console.WriteLine($"ETH/BTC margin: {ethSymbol?.MarginTrading}");

var orderBook = await restClient.GetOrderBookAsync("BTCUSDT", depth: 10);
Console.WriteLine($"BTCUSDT Best Ask: {orderBook.Asks[0].Price}, Best Bid: {orderBook.Bids[0].Price}");
orderBook.Dispose();

var candles = await restClient.GetCandlesAsync("BTCUSDT", CandlePeriod.H1, limit: 5);
foreach (var c in candles)
{
    Console.WriteLine($"{c.Timestamp}: O={c.Open} H={c.High} L={c.Low} C={c.Close} V={c.Volume}");
}

// Спот ордера
var spotOrders = await restClient.GetActiveSpotOrdersAsync();
Console.WriteLine($"Active spot orders: {spotOrders.Length}");

var newOrder = await restClient.CreateSpotOrderAsync(new CreateSpotOrderRequest
{
    Symbol = "OPENUSDT",
    Side = OrderSide.Buy,
    Quantity = 1,
    Price = 0.000010m,
    Type = OrderType.Limit,
    PostOnly = false
});
Console.WriteLine($"Created: {newOrder.ClientOrderId}, status={newOrder.Status}");

var canceled = await restClient.CancelSpotOrderAsync(newOrder.ClientOrderId);
Console.WriteLine($"Canceled: {canceled.Status}");

// Фьючерсы

var futureOrder = await restClient.CreateFuturesOrderAsync( new CreateFuturesOrderRequest
{
    Symbol = "XRPUSDT_PERP",
    Side = OrderSide.Buy,
    Quantity = 1,
    Price = 0.0001m,
    Type = OrderType.Limit
});

var canceled = await restClient.CancelFuturesOrderAsync(futureOrder.ClientOrderId);
Console.WriteLine($"Canceled: {canceled.Status}");


// Создание нового isolated margin account с leverage 50x
var account = await restClient.CreateOrUpdateMarginAccountAsync(
    MarginMode.Isolated,
    "XRPUSDT_PERP",
    marginBalance: 0.15m,     // 100 USDT
    leverage: 75m,           // 50x
    strictValidate: true
);

Console.WriteLine($"Created account: {account.Symbol}");
Console.WriteLine($"Leverage: {account.Leverage}x");
Console.WriteLine($"Balance: {account.Currencies[0].MarginBalance} {account.Currencies[0].Code}");

// Изменение leverage
account = await restClient.UpdateLeverageAsync(
    "XRPUSDT_PERP",
    leverage: 75m  // Меняем на 75x
);
Console.WriteLine($"New leverage: {account.Leverage}x");

//Добавление маржи
account = await restClient.AddMarginAsync(
    MarginMode.Isolated,
    "XRPUSDT_PERP",
   amount: 0.1m  // Добавляем 50 USDT
);
Console.WriteLine($"New balance: {account.Currencies[0].MarginBalance}");

// Вывод части маржи
account = await restClient.RemoveMarginAsync(
    MarginMode.Isolated,
    "XRPUSDT_PERP",
    amount: 0.1m  // Выводим 0.1 USDT
);
Console.WriteLine($"Balance after withdrawal: {account.Currencies[0].MarginBalance}");

// Закрытие аккаунта (вывод всех средств)
account = await restClient.CloseMarginAccountAsync(
    MarginMode.Isolated,
    "XRPUSDT_PERP"
);
Console.WriteLine($"Account closed. Balance: {account.Currencies[0].MarginBalance}");

// Через TradingEngine
await using var engine = new TradingEngine("api-key", "token");
await engine.StartAsync();

// Создаём аккаунт и сразу торгуем
var marginAccount = await engine.CreateOrUpdateMarginAccountAsync(
    MarginMode.Isolated,
    "XRPUSDT_PERP",
    marginBalance: 0.1m,
    leverage: 75m
);

var futuresOrder = await engine.PlaceFuturesOrderAsync(new CreateFuturesOrderRequest
{
    Symbol = "XRPUSDT_PERP",
    Side = OrderSide.Buy,
    Quantity = 1,
    Price = 0.0001m,
    Type = OrderType.Limit,
    MarginMode = MarginMode.Isolated
});

Console.WriteLine($"Futures order placed: {futuresOrder.ClientOrderId}");


var futuresBalances = await restClient.GetFuturesBalanceAsync();
foreach (var b in futuresBalances)
{
    Console.WriteLine($"{b.Currency}: available={b.Available}");
}

var newFutureOrder = await restClient.CreateFuturesOrderAsync(new CreateFuturesOrderRequest
{
    Symbol = "XRPUSDT",
    Side = OrderSide.Buy,
    Quantity = 1,
    Price = 0.0001m,
    Type = OrderType.Limit,
    PostOnly = false
});
Console.WriteLine($"Created: {newFutureOrder.ClientOrderId}, status={newFutureOrder.Status}");

var canceledFutureOrder = await restClient.CancelFuturesOrderAsync(newFutureOrder.ClientOrderId);
Console.WriteLine($"Canceled: {newFutureOrder.Status}");

// ============ ПРИМЕР 2: MarketData WebSocket ============

var marketData = new MarketDataManager();
await marketData.StartAsync();

marketData.OrderBookChanged += (symbol, ob) =>
{
    if (ob.AskCount > 0 && ob.BidCount > 0)
    {
        Console.WriteLine(
            $"[OB] {symbol} ask={ob.Asks[0].Price} bid={ob.Bids[0].Price} " +
            $"spread={ob.Asks[0].Price - ob.Bids[0].Price}");
    }
};

marketData.TradesReceived += (symbol, trades) =>
{
    foreach (var t in trades)
    {
        Console.WriteLine($"[Trade] {symbol} {t.Side} {t.Quantity}@{t.Price}");
    }
};

await marketData.SubscribeOrderBookAsync("BTCUSDT");
await marketData.SubscribeTradesAsync("BTCUSDT");

// ============ ПРИМЕР 3: Trading Engine ============



engine.SpotOrderPlaced += o =>
    Console.WriteLine($"[ENGINE] Placed: {o.ClientOrderId} {o.Side} {o.Quantity}@{o.Price}");

engine.SpotOrderFilled += o =>
    Console.WriteLine($"[ENGINE] Filled: {o.ClientOrderId}");

engine.SpotOrderCanceled += o =>
    Console.WriteLine($"[ENGINE] Canceled: {o.ClientOrderId}");

engine.Error += ex =>
    Console.WriteLine($"[ENGINE ERROR] {ex.Message}");

await engine.StartAsync();

Console.WriteLine($"Active spot orders: {engine.ActiveSpotOrders.Count}");

// Размещение ордера через engine
var engineOrder = await engine.PlaceSpotOrderAsync(new CreateSpotOrderRequest
{
    Symbol = "BTCUSDT",
    Side = OrderSide.Buy,
    Quantity = 0.00001m,
    Price = 20000.00m,
    Type = OrderType.Limit,
    PostOnly = true
});

Console.WriteLine($"Engine order: {engineOrder.ClientOrderId}");

// Замена ордера
var replaced = await engine.ReplaceSpotOrderAsync(
    engineOrder.ClientOrderId,
    new ReplaceSpotOrderRequest
    {
        Quantity = 0.00002m,
        Price = 20001.00m
    });

Console.WriteLine($"Replaced: {replaced.ClientOrderId}");

// Отмена
await engine.CancelSpotOrderAsync(replaced.ClientOrderId);

// Фьючерсы через engine
var accounts = await engine.GetFuturesAccountsAsync();
foreach (var acc in accounts)
{
    Console.WriteLine($"Futures account: {acc.Symbol}, leverage={acc.Leverage}");
    if (acc.Positions is not null)
    {
        foreach (var pos in acc.Positions)
        {
            Console.WriteLine($"  Position: {pos.Quantity}@{pos.PriceEntry}, PnL={pos.Pnl}");
        }
    }
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();

await marketData.DisposeAsync();
