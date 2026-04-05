// Fixtures/TestData.cs
using HitBTC.Connector.Core.Models;

namespace HitBTC.Connector.Tests.Fixtures;

public static class TestData
{
    public static class Json
    {
        public const string SymbolResponse = """
        {
            "BTCUSDT": {
                "type": "spot",
                "base_currency": "BTC",
                "quote_currency": "USDT",
                "status": "working",
                "quantity_increment": "0.00001",
                "tick_size": "0.01",
                "take_rate": "0.0025",
                "make_rate": "0.001",
                "fee_currency": "USDT",
                "margin_trading": true,
                "max_initial_leverage": "10.00"
            }
        }
        """;

        public const string OrderBookResponse = """
        {
            "timestamp": "2024-01-15T10:30:00.000Z",
            "ask": [
                ["50001.50", "1.5"],
                ["50002.00", "2.0"],
                ["50003.00", "0.5"]
            ],
            "bid": [
                ["50000.00", "1.0"],
                ["49999.50", "2.5"],
                ["49998.00", "1.2"]
            ]
        }
        """;

        public const string SpotOrderResponse = """
        {
            "id": 12345678,
            "client_order_id": "test_order_001",
            "symbol": "BTCUSDT",
            "side": "buy",
            "status": "new",
            "type": "limit",
            "time_in_force": "GTC",
            "quantity": "0.001",
            "price": "50000.00",
            "quantity_cumulative": "0",
            "post_only": true,
            "created_at": "2024-01-15T10:30:00.000Z",
            "updated_at": "2024-01-15T10:30:00.000Z"
        }
        """;

        public const string SpotOrdersArrayResponse = """
        [
            {
                "id": 12345678,
                "client_order_id": "test_order_001",
                "symbol": "BTCUSDT",
                "side": "buy",
                "status": "new",
                "type": "limit",
                "time_in_force": "GTC",
                "quantity": "0.001",
                "price": "50000.00",
                "quantity_cumulative": "0",
                "post_only": false,
                "created_at": "2024-01-15T10:30:00.000Z",
                "updated_at": "2024-01-15T10:30:00.000Z"
            }
        ]
        """;

        public const string CandlesResponse = """
        [
            {
                "timestamp": "2024-01-15T10:00:00.000Z",
                "open": "50000.00",
                "close": "50100.00",
                "min": "49950.00",
                "max": "50150.00",
                "volume": "100.5",
                "volume_quote": "5025000.00"
            },
            {
                "timestamp": "2024-01-15T11:00:00.000Z",
                "open": "50100.00",
                "close": "50200.00",
                "min": "50050.00",
                "max": "50250.00",
                "volume": "85.2",
                "volume_quote": "4270000.00"
            }
        ]
        """;

        public const string FuturesBalanceResponse = """
        [
            {
                "currency": "USDT",
                "available": "1000.50",
                "reserved": "100.00",
                "reserved_margin": "50.00",
                "cross_margin_reserved": "0"
            }
        ]
        """;

        public const string FuturesAccountResponse = """
        {
            "symbol": "BTCUSDT_PERP",
            "type": "isolated",
            "leverage": "50.00",
            "created_at": "2024-01-15T10:00:00.000Z",
            "updated_at": "2024-01-15T10:30:00.000Z",
            "currencies": [
                {
                    "code": "USDT",
                    "margin_balance": "500.00",
                    "reserved_orders": "100.00",
                    "reserved_positions": "200.00"
                }
            ],
            "positions": [
                {
                    "id": 123456,
                    "symbol": "BTCUSDT_PERP",
                    "quantity": "0.01",
                    "margin_mode": "isolated",
                    "price_entry": "50000.00",
                    "price_margin_call": "45000.00",
                    "price_liquidation": "44000.00",
                    "pnl": "50.00",
                    "created_at": "2024-01-15T10:00:00.000Z",
                    "updated_at": "2024-01-15T10:30:00.000Z"
                }
            ],
            "margin_call": false
        }
        """;

        public const string ErrorResponse = """
        {
            "error": {
                "code": 20001,
                "message": "Insufficient funds",
                "description": "Check that the funds are sufficient, given commissions"
            }
        }
        """;

        public const string OrderNotFoundError = """
        {
            "error": {
                "code": 20002,
                "message": "Order not found",
                "description": ""
            }
        }
        """;
    }

    public static CreateSpotOrderRequest ValidSpotOrderRequest => new()
    {
        Symbol = "BTCUSDT",
        Side = OrderSide.Buy,
        Quantity = 0.001m,
        Price = 50000m,
        Type = OrderType.Limit,
        TimeInForce = TimeInForce.GTC,
        PostOnly = true
    };

    public static CreateFuturesOrderRequest ValidFuturesOrderRequest => new()
    {
        Symbol = "BTCUSDT_PERP",
        Side = OrderSide.Buy,
        Quantity = 0.01m,
        Price = 50000m,
        Type = OrderType.Limit,
        MarginMode = MarginMode.Isolated
    };
}