using System.Text.Json.Serialization;

namespace HitBTC.Connector.Core.Models;

public sealed class FuturesMarginAccount
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("leverage")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
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
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal MarginBalance { get; set; }

    [JsonPropertyName("reserved_orders")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal ReservedOrders { get; set; }

    [JsonPropertyName("reserved_positions")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal ReservedPositions { get; set; }

    [JsonPropertyName("margin_call_margin")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? MarginCallMargin { get; set; }

    [JsonPropertyName("liquidation_margin")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? LiquidationMargin { get; set; }

    [JsonPropertyName("debt_sum")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? DebtSum { get; set; }
}

public sealed class MarginPosition
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Quantity { get; set; }

    [JsonPropertyName("margin_mode")]
    public string MarginMode { get; set; } = string.Empty;

    [JsonPropertyName("price_entry")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal PriceEntry { get; set; }

    [JsonPropertyName("price_margin_call")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal PriceMarginCall { get; set; }

    [JsonPropertyName("price_liquidation")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal PriceLiquidation { get; set; }

    [JsonPropertyName("pnl")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Pnl { get; set; }

    [JsonPropertyName("fee_cumulative")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? FeeCumulative { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}