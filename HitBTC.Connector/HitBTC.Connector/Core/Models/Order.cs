// Core/Models/Order.cs
using System.Text.Json.Serialization;

using HitBTC.Connector.Core.Converters;

namespace HitBTC.Connector.Core.Models;

public sealed class SpotOrder
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("client_order_id")]
    public string ClientOrderId { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("side")]
    public OrderSide Side { get; set; }

    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

    [JsonPropertyName("type")]
    public OrderType Type { get; set; }

    [JsonPropertyName("time_in_force")]
    public TimeInForce TimeInForce { get; set; }

    [JsonPropertyName("quantity")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Quantity { get; set; }

    [JsonPropertyName("price")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? Price { get; set; }

    [JsonPropertyName("stop_price")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? StopPrice { get; set; }

    [JsonPropertyName("quantity_cumulative")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal QuantityCumulative { get; set; }

    [JsonPropertyName("post_only")]
    public bool PostOnly { get; set; }

    [JsonPropertyName("display_quantity")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? DisplayQuantity { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public sealed class FuturesOrder
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("client_order_id")]
    public string ClientOrderId { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("side")]
    public OrderSide Side { get; set; }

    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

    [JsonPropertyName("type")]
    public OrderType Type { get; set; }

    [JsonPropertyName("time_in_force")]
    public TimeInForce TimeInForce { get; set; }

    [JsonPropertyName("quantity")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Quantity { get; set; }

    [JsonPropertyName("price")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? Price { get; set; }

    [JsonPropertyName("stop_price")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? StopPrice { get; set; }

    [JsonPropertyName("quantity_cumulative")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal QuantityCumulative { get; set; }

    [JsonPropertyName("post_only")]
    public bool PostOnly { get; set; }

    [JsonPropertyName("reduce_only")]
    public bool ReduceOnly { get; set; }

    [JsonPropertyName("close_position")]
    public bool ClosePosition { get; set; }

    [JsonPropertyName("margin_mode")]
    public string MarginMode { get; set; } = "isolated";

    [JsonPropertyName("display_quantity")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? DisplayQuantity { get; set; }

    [JsonPropertyName("price_average")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? PriceAverage { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("trades")]
    public FuturesTrade[]? Trades { get; set; }
}

public sealed class FuturesTrade
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("position_id")]
    public long PositionId { get; set; }

    [JsonPropertyName("quantity")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Quantity { get; set; }

    [JsonPropertyName("price")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Price { get; set; }

    [JsonPropertyName("fee")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Fee { get; set; }

    [JsonPropertyName("taker")]
    public bool Taker { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}