// Core/Models/Symbol.cs
using System.Text.Json.Serialization;

using HitBTC.Connector.Core.Converters;

namespace HitBTC.Connector.Core.Models;

public sealed class SymbolInfo
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("base_currency")]
    public string? BaseCurrency { get; set; }

    [JsonPropertyName("quote_currency")]
    public string QuoteCurrency { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("quantity_increment")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal QuantityIncrement { get; set; }

    [JsonPropertyName("tick_size")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal TickSize { get; set; }

    [JsonPropertyName("take_rate")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal TakeRate { get; set; }

    [JsonPropertyName("make_rate")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal MakeRate { get; set; }

    [JsonPropertyName("fee_currency")]
    public string FeeCurrency { get; set; } = string.Empty;

    [JsonPropertyName("margin_trading")]
    public bool MarginTrading { get; set; }

    [JsonPropertyName("max_initial_leverage")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? MaxInitialLeverage { get; set; }

    [JsonPropertyName("contract_type")]
    public string? ContractType { get; set; }

    [JsonPropertyName("underlying")]
    public string? Underlying { get; set; }

    [JsonPropertyName("expiry")]
    public DateTime? Expiry { get; set; }
}