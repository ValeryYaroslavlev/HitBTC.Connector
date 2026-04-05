// Core/Models/Candle.cs
using System.Text.Json.Serialization;

using HitBTC.Connector.Core.Converters;

namespace HitBTC.Connector.Core.Models;

public sealed class Candle
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("open")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Open { get; set; }

    [JsonPropertyName("close")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Close { get; set; }

    [JsonPropertyName("min")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Low { get; set; }

    [JsonPropertyName("max")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal High { get; set; }

    [JsonPropertyName("volume")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Volume { get; set; }

    [JsonPropertyName("volume_quote")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal VolumeQuote { get; set; }
}