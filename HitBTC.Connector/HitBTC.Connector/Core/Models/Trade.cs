// Core/Models/Trade.cs
using System.Text.Json.Serialization;

using HitBTC.Connector.Core.Converters;

namespace HitBTC.Connector.Core.Models;

public sealed class PublicTrade
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("price")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Price { get; set; }

    [JsonPropertyName("quantity")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Quantity { get; set; }

    [JsonPropertyName("side")]
    public OrderSide Side { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}