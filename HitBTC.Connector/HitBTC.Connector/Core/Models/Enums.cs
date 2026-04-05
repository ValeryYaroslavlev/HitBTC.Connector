// Core/Models/Enums.cs
using System.Text.Json.Serialization;

namespace HitBTC.Connector.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter<OrderSide>))]
public enum OrderSide
{
    [JsonStringEnumMemberName("buy")]
    Buy,

    [JsonStringEnumMemberName("sell")]
    Sell
}

[JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))]
public enum OrderStatus
{
    [JsonStringEnumMemberName("new")]
    New,

    [JsonStringEnumMemberName("suspended")]
    Suspended,

    [JsonStringEnumMemberName("partiallyFilled")]
    PartiallyFilled,

    [JsonStringEnumMemberName("filled")]
    Filled,

    [JsonStringEnumMemberName("canceled")]
    Canceled,

    [JsonStringEnumMemberName("expired")]
    Expired
}

[JsonConverter(typeof(JsonStringEnumConverter<OrderType>))]
public enum OrderType
{
    [JsonStringEnumMemberName("limit")]
    Limit,

    [JsonStringEnumMemberName("market")]
    Market,

    [JsonStringEnumMemberName("stopLimit")]
    StopLimit,

    [JsonStringEnumMemberName("stopMarket")]
    StopMarket,

    [JsonStringEnumMemberName("takeProfitLimit")]
    TakeProfitLimit,

    [JsonStringEnumMemberName("takeProfitMarket")]
    TakeProfitMarket
}

[JsonConverter(typeof(JsonStringEnumConverter<TimeInForce>))]
public enum TimeInForce
{
    [JsonStringEnumMemberName("GTC")]
    GTC,

    [JsonStringEnumMemberName("IOC")]
    IOC,

    [JsonStringEnumMemberName("FOK")]
    FOK,

    [JsonStringEnumMemberName("Day")]
    Day,

    [JsonStringEnumMemberName("GTD")]
    GTD
}

[JsonConverter(typeof(JsonStringEnumConverter<SymbolType>))]
public enum SymbolType
{
    [JsonStringEnumMemberName("spot")]
    Spot,

    [JsonStringEnumMemberName("futures")]
    Futures
}

[JsonConverter(typeof(JsonStringEnumConverter<SymbolStatus>))]
public enum SymbolStatus
{
    [JsonStringEnumMemberName("working")]
    Working,

    [JsonStringEnumMemberName("suspended")]
    Suspended,

    [JsonStringEnumMemberName("clearing")]
    Clearing
}

[JsonConverter(typeof(JsonStringEnumConverter<MarginMode>))]
public enum MarginMode
{
    [JsonStringEnumMemberName("isolated")]
    Isolated,

    [JsonStringEnumMemberName("cross")]
    Cross
}

public enum WebSocketEndpoint
{
    Public,
    Trading,
    Wallet
}

[JsonConverter(typeof(JsonStringEnumConverter<CandlePeriod>))]
public enum CandlePeriod
{
    [JsonStringEnumMemberName("M1")]
    M1,

    [JsonStringEnumMemberName("M3")]
    M3,

    [JsonStringEnumMemberName("M5")]
    M5,

    [JsonStringEnumMemberName("M15")]
    M15,

    [JsonStringEnumMemberName("M30")]
    M30,

    [JsonStringEnumMemberName("H1")]
    H1,

    [JsonStringEnumMemberName("H4")]
    H4,

    [JsonStringEnumMemberName("D1")]
    D1,

    [JsonStringEnumMemberName("D7")]
    D7,

    [JsonStringEnumMemberName("1M")]
    Month1
}

[JsonConverter(typeof(JsonStringEnumConverter<SortDirection>))]
public enum SortDirection
{
    [JsonStringEnumMemberName("ASC")]
    Asc,

    [JsonStringEnumMemberName("DESC")]
    Desc
}