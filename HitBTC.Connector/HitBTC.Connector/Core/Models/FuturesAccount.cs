// Core/Models/FuturesAccount.cs
using System.Text.Json.Serialization;

using HitBTC.Connector.Core.Converters;

namespace HitBTC.Connector.Core.Models;

public sealed class FuturesMarginAccount
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("leverage")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? Leverage { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("currencies")]
    public MarginCurrency[] Currencies { get; set; } = Array.Empty<MarginCurrency>();

    [JsonPropertyName("positions")]
    public MarginPosition[]? Positions { get; set; }

    [JsonPropertyName("margin_call")]
    public bool MarginCall { get; set; }
}

public sealed class MarginCurrency
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

    [JsonPropertyName("margin_call_margin")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? MarginCallMargin { get; set; }

    [JsonPropertyName("liquidation_margin")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? LiquidationMargin { get; set; }

    [JsonPropertyName("debt_sum")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? DebtSum { get; set; }
}

public sealed class MarginPosition
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Quantity { get; set; }

    [JsonPropertyName("leverage")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? Leverage { get; set; }

    [JsonPropertyName("margin_mode")]
    public string MarginMode { get; set; } = string.Empty;

    [JsonPropertyName("price_entry")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal PriceEntry { get; set; }

    [JsonPropertyName("price_margin_call")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal PriceMarginCall { get; set; }

    [JsonPropertyName("price_liquidation")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal PriceLiquidation { get; set; }

    [JsonPropertyName("pnl")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Pnl { get; set; }

    [JsonPropertyName("fee_cumulative")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? FeeCumulative { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}