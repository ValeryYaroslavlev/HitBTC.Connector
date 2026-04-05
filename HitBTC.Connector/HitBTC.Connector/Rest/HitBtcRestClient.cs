// Rest/HitBtcRestClient.cs
using System.Buffers;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using HitBTC.Connector.Core.Auth;
using HitBTC.Connector.Core.Infrastructure;
using HitBTC.Connector.Core.Models;

namespace HitBTC.Connector.Rest;

public sealed class HitBtcRestClient : IHitBtcRestClient
{
    // ВАЖНО: BaseAddress должен заканчиваться на /
    private const string BaseUrl = "https://api.hitbtc.com/api/3/";

    private readonly HttpClient _publicClient;
    private readonly HttpClient _authClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public HitBtcRestClient(string apiKey, string secretKey, int? authWindow = null)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        var publicHandler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 20,
            EnableMultipleHttp2Connections = true,
            UseProxy = false,
            UseCookies = false
        };

        _publicClient = new HttpClient(publicHandler)
        {
            BaseAddress = new Uri(BaseUrl),  // Заканчивается на /
            Timeout = TimeSpan.FromSeconds(30)
        };

        var authHandler = new HmacAuthHandler(apiKey, secretKey, authWindow);
        _authClient = new HttpClient(authHandler)
        {
            BaseAddress = new Uri(BaseUrl),  // Заканчивается на /
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    // =================== PUBLIC API ===================

    public async Task<Dictionary<string, SymbolInfo>> GetSymbolsAsync(
        string[]? symbols = null, CancellationToken ct = default)
    {
        Span<char> urlBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(urlBuf);

        // БЕЗ / в начале - относительный путь
        sb.Append("public/symbol");

        if (symbols is { Length: > 0 })
        {
            sb.Append("?symbols=");
            sb.Append(string.Join(",", symbols));
        }

        return await PublicGetAsync<Dictionary<string, SymbolInfo>>(sb.ToString(), ct)
               ?? new Dictionary<string, SymbolInfo>();
    }

    public async Task<SymbolInfo?> GetSymbolAsync(string symbol, CancellationToken ct = default)
    {
        // БЕЗ / в начале
        return await PublicGetAsync<SymbolInfo>($"public/symbol/{symbol}", ct);
    }

    public async Task<OrderBook> GetOrderBookAsync(
        string symbol, int depth = 100, CancellationToken ct = default)
    {
        var snapshot = await PublicGetAsync<OrderBookSnapshot>(
            $"public/orderbook/{symbol}?depth={depth}", ct);

        if (snapshot is null)
            return new OrderBook();

        return OrderBook.FromSnapshot(snapshot);
    }

    public async Task<Candle[]> GetCandlesAsync(
        string symbol,
        CandlePeriod period = CandlePeriod.M30,
        SortDirection sort = SortDirection.Desc,
        DateTime? from = null,
        DateTime? till = null,
        int limit = 100,
        int offset = 0,
        CancellationToken ct = default)
    {
        Span<char> urlBuf = stackalloc char[512];
        var sb = new ValueStringBuilder(urlBuf);

        // БЕЗ / в начале
        sb.Append("public/candles/");
        sb.Append(symbol);
        sb.Append("?limit=");
        sb.Append(limit.ToString());
        sb.Append("&offset=");
        sb.Append(offset.ToString());

        var periodStr = period switch
        {
            CandlePeriod.M1 => "M1",
            CandlePeriod.M3 => "M3",
            CandlePeriod.M5 => "M5",
            CandlePeriod.M15 => "M15",
            CandlePeriod.M30 => "M30",
            CandlePeriod.H1 => "H1",
            CandlePeriod.H4 => "H4",
            CandlePeriod.D1 => "D1",
            CandlePeriod.D7 => "D7",
            CandlePeriod.Month1 => "1M",
            _ => "M30"
        };
        sb.Append("&period=");
        sb.Append(periodStr);

        var sortStr = sort == SortDirection.Asc ? "ASC" : "DESC";
        sb.Append("&sort=");
        sb.Append(sortStr);

        if (from.HasValue)
        {
            sb.Append("&from=");
            sb.Append(from.Value.ToString("O"));
        }
        if (till.HasValue)
        {
            sb.Append("&till=");
            sb.Append(till.Value.ToString("O"));
        }

        return await PublicGetAsync<Candle[]>(sb.ToString(), ct)
               ?? Array.Empty<Candle>();
    }

    // =================== SPOT API ===================

    public async Task<SpotOrder[]> GetActiveSpotOrdersAsync(
        string? symbol = null, CancellationToken ct = default)
    {
        var url = symbol is not null
            ? $"spot/order?symbol={symbol}"
            : "spot/order";

        return await AuthGetAsync<SpotOrder[]>(url, ct)
               ?? Array.Empty<SpotOrder>();
    }

    public async Task<SpotOrder> GetSpotOrderAsync(
        string clientOrderId, CancellationToken ct = default)
    {
        return (await AuthGetAsync<SpotOrder>($"spot/order/{clientOrderId}", ct))!;
    }

    public async Task<SpotOrder> CreateSpotOrderAsync(
        CreateSpotOrderRequest request, CancellationToken ct = default)
    {
        var form = BuildSpotOrderForm(request);
        return (await AuthPostFormAsync<SpotOrder>("spot/order", form, ct))!;
    }

    public async Task<SpotOrder> ReplaceSpotOrderAsync(
            string clientOrderId, ReplaceSpotOrderRequest request, CancellationToken ct = default)
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("quantity", request.Quantity.ToApiString())
        };

        if (request.Price.HasValue)
            pairs.Add(new("price", request.Price.Value.ToApiString()));
        if (request.StopPrice.HasValue)
            pairs.Add(new("stop_price", request.StopPrice.Value.ToApiString()));
        if (request.NewClientOrderId is not null)
            pairs.Add(new("new_client_order_id", request.NewClientOrderId));
        if (request.StrictValidate)
            pairs.Add(new("strict_validate", "true"));

        return (await AuthPatchFormAsync<SpotOrder>(
            $"spot/order/{clientOrderId}", pairs, ct))!;
    }

    public async Task<SpotOrder> CancelSpotOrderAsync(
        string clientOrderId, CancellationToken ct = default)
    {
        return (await AuthDeleteAsync<SpotOrder>(
            $"spot/order/{clientOrderId}", ct))!;
    }

    public async Task<SpotOrder[]> CancelAllSpotOrdersAsync(
        string? symbol = null, CancellationToken ct = default)
    {
        var url = symbol is not null
            ? $"spot/order?symbol={symbol}"
            : "spot/order";

        return await AuthDeleteAsync<SpotOrder[]>(url, ct)
               ?? Array.Empty<SpotOrder>();
    }

    // =================== FUTURES API ===================

    public async Task<FuturesBalance[]> GetFuturesBalanceAsync(CancellationToken ct = default)
    {
        return await AuthGetAsync<FuturesBalance[]>("futures/balance", ct)
               ?? Array.Empty<FuturesBalance>();
    }

    public async Task<FuturesBalance?> GetFuturesBalanceAsync(
        string currency, CancellationToken ct = default)
    {
        return await AuthGetAsync<FuturesBalance>($"futures/balance/{currency}", ct);
    }

    public async Task<FuturesMarginAccount[]> GetFuturesAccountsAsync(CancellationToken ct = default)
    {
        return await AuthGetAsync<FuturesMarginAccount[]>("futures/account", ct)
               ?? Array.Empty<FuturesMarginAccount>();
    }

    public async Task<FuturesMarginAccount> GetFuturesAccountAsync(
        string symbol, CancellationToken ct = default)
    {
        return (await AuthGetAsync<FuturesMarginAccount>(
            $"futures/account/isolated/{symbol}", ct))!;
    }

    public async Task<FuturesMarginAccount> UpdateFuturesAccountAsync(
        MarginMode marginMode,
        string symbol,
        UpdateMarginAccountRequest request,
        CancellationToken ct = default)
    {
        return await CreateOrUpdateMarginAccountAsync(
            marginMode,
            symbol,
            request.MarginBalance,
            request.Leverage,
            request.StrictValidate,
            ct);
    }

    public async Task<MarginPosition[]> CloseAllFuturesPositionsAsync(CancellationToken ct = default)
    {
        return await AuthDeleteAsync<MarginPosition[]>("futures/position", ct)
               ?? Array.Empty<MarginPosition>();
    }

    public async Task<MarginPosition[]> CloseFuturesPositionAsync(
            MarginMode marginMode, string symbol,
            ClosePositionRequest? request = null, CancellationToken ct = default)
    {
        var mode = marginMode == MarginMode.Isolated ? "isolated" : "cross";
        var url = $"futures/position/{mode}/{symbol}";

        if (request?.Price.HasValue == true || request?.StrictValidate == true)
        {
            using var msg = new HttpRequestMessage(HttpMethod.Delete, url);
            var pairs = new List<KeyValuePair<string, string>>();

            if (request.Price.HasValue)
                pairs.Add(new("price", request.Price.Value.ToApiString()));
            if (request.StrictValidate)
                pairs.Add(new("strict_validate", "true"));

            msg.Content = new FormUrlEncodedContent(pairs);
            return await SendAuthAsync<MarginPosition[]>(msg, ct)
                   ?? Array.Empty<MarginPosition>();
        }

        return await AuthDeleteAsync<MarginPosition[]>(url, ct)
               ?? Array.Empty<MarginPosition>();
    }

    public async Task<FuturesOrder[]> GetActiveFuturesOrdersAsync(
        string? symbol = null, CancellationToken ct = default)
    {
        var url = symbol is not null
            ? $"futures/order?symbol={symbol}"
            : "futures/order";

        return await AuthGetAsync<FuturesOrder[]>(url, ct)
               ?? Array.Empty<FuturesOrder>();
    }

    public async Task<FuturesOrder> GetFuturesOrderAsync(
        string clientOrderId, CancellationToken ct = default)
    {
        return (await AuthGetAsync<FuturesOrder>(
            $"futures/order/{clientOrderId}", ct))!;
    }

    public async Task<FuturesOrder> CreateFuturesOrderAsync(
        CreateFuturesOrderRequest request, CancellationToken ct = default)
    {
        var form = BuildFuturesOrderForm(request);
        return (await AuthPostFormAsync<FuturesOrder>("futures/order", form, ct))!;
    }
    public async Task<FuturesOrder> ReplaceFuturesOrderAsync(
            string clientOrderId, ReplaceFuturesOrderRequest request, CancellationToken ct = default)
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("quantity", request.Quantity.ToApiString())
        };

        if (request.Price.HasValue)
            pairs.Add(new("price", request.Price.Value.ToApiString()));
        if (request.StopPrice.HasValue)
            pairs.Add(new("stop_price", request.StopPrice.Value.ToApiString()));
        if (request.NewClientOrderId is not null)
            pairs.Add(new("new_client_order_id", request.NewClientOrderId));
        if (request.StrictValidate)
            pairs.Add(new("strict_validate", "true"));

        return (await AuthPatchFormAsync<FuturesOrder>(
            $"futures/order/{clientOrderId}", pairs, ct))!;
    }

    public async Task<FuturesOrder> CancelFuturesOrderAsync(
        string clientOrderId, CancellationToken ct = default)
    {
        return (await AuthDeleteAsync<FuturesOrder>(
            $"futures/order/{clientOrderId}", ct))!;
    }

    public async Task<FuturesOrder[]> CancelAllFuturesOrdersAsync(
        string? symbol = null, CancellationToken ct = default)
    {
        var url = symbol is not null
            ? $"futures/order?symbol={symbol}"
            : "futures/order";

        return await AuthDeleteAsync<FuturesOrder[]>(url, ct)
               ?? Array.Empty<FuturesOrder>();
    }

    // =================== FUTURES MARGIN ACCOUNT ===================

    /// <summary>
    /// Creates or updates a futures margin account.
    /// Setting margin_balance to 0 closes the account and returns funds to trading account.
    /// </summary>
    /// <param name="marginMode">Margin mode: isolated or cross</param>
    /// <param name="symbol">Contract symbol (e.g., BTCUSDT_PERP)</param>
    /// <param name="marginBalance">Amount of currency to reserve. Set to 0 to close account.</param>
    /// <param name="leverage">Leverage (1-1000). Required for isolated mode when balance is 0. Not allowed for cross mode.</param>
    /// <param name="strictValidate">Check margin_balance format and precision</param>
    /// <param name="ct">Cancellation token</param>
    public async Task<FuturesMarginAccount> CreateOrUpdateMarginAccountAsync(
        MarginMode marginMode,
        string symbol,
        decimal marginBalance,
        decimal? leverage = null,
        bool strictValidate = false,
        CancellationToken ct = default)
    {
        var mode = marginMode == MarginMode.Isolated ? "isolated" : "cross";

        var pairs = new List<KeyValuePair<string, string>>
        {
            new("margin_balance", marginBalance.ToApiString())
        };

        // Leverage не разрешён для cross margin
        if (leverage.HasValue)
        {
            if (marginMode == MarginMode.Cross)
            {
                throw new ArgumentException(
                    "Leverage cannot be specified for cross margin mode. " +
                    "Use POST /api/3/margin/position/leverage instead.",
                    nameof(leverage));
            }

            if (leverage.Value < 1 || leverage.Value > 1000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(leverage),
                    leverage.Value,
                    "Leverage must be between 1 and 1000");
            }

            pairs.Add(new("leverage", leverage.Value.ToApiString()));
        }

        if (strictValidate)
        {
            pairs.Add(new("strict_validate", "true"));
        }

        return (await AuthPutFormAsync<FuturesMarginAccount>(
            $"futures/account/{mode}/{symbol}", pairs, ct))!;
    }

    /// <summary>
    /// Withdraws all funds from margin account by setting margin_balance to 0.
    /// </summary>
    public async Task<FuturesMarginAccount> CloseMarginAccountAsync(
        MarginMode marginMode,
        string symbol,
        CancellationToken ct = default)
    {
        return await CreateOrUpdateMarginAccountAsync(
            marginMode,
            symbol,
            marginBalance: 0m,
            leverage: null,
            strictValidate: false,
            ct);
    }

    /// <summary>
    /// Updates leverage for an existing isolated margin account.
    /// </summary>
    public async Task<FuturesMarginAccount> UpdateLeverageAsync(
        string symbol,
        decimal leverage,
        CancellationToken ct = default)
    {
        if (leverage < 1 || leverage > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leverage),
                leverage,
                "Leverage must be between 1 and 1000");
        }

        // Получаем текущий аккаунт чтобы узнать текущий баланс
        var account = await GetFuturesAccountAsync(symbol, ct);

        var currentBalance = account.Currencies.Length > 0
            ? account.Currencies[0].MarginBalance
            : 0m;

        return await CreateOrUpdateMarginAccountAsync(
            MarginMode.Isolated,
            symbol,
            currentBalance,
            leverage,
            strictValidate: false,
            ct);
    }

    /// <summary>
    /// Adds funds to an existing margin account.
    /// </summary>
    public async Task<FuturesMarginAccount> AddMarginAsync(
        MarginMode marginMode,
        string symbol,
        decimal amount,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Amount must be positive");
        }

        // Получаем текущий аккаунт
        var mode = marginMode == MarginMode.Isolated ? "isolated" : "cross";
        FuturesMarginAccount? account = null;

        try
        {
            account = await GetFuturesAccountAsync(symbol, ct);
        }
        catch (HitBtcApiException ex) when (ex.StatusCode == 404)
        {
            // Аккаунт не существует — создаём новый
        }

        var currentBalance = account?.Currencies.Length > 0
            ? account.Currencies[0].MarginBalance
            : 0m;

        var newBalance = currentBalance + amount;

        // Для нового isolated аккаунта нужен leverage
        decimal? leverage = null;
        if (account is null && marginMode == MarginMode.Isolated)
        {
            leverage = 10m; // Default leverage
        }

        return await CreateOrUpdateMarginAccountAsync(
            marginMode,
            symbol,
            newBalance,
            leverage,
            strictValidate: false,
            ct);
    }

    /// <summary>
    /// Removes funds from margin account.
    /// </summary>
    public async Task<FuturesMarginAccount> RemoveMarginAsync(
        MarginMode marginMode,
        string symbol,
        decimal amount,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Amount must be positive");
        }

        var account = await GetFuturesAccountAsync(symbol, ct);

        var currentBalance = account.Currencies.Length > 0
            ? account.Currencies[0].MarginBalance
            : 0m;

        var newBalance = currentBalance - amount;

        if (newBalance < 0)
        {
            throw new InvalidOperationException(
                $"Cannot remove {amount}. Current balance is {currentBalance}");
        }

        return await CreateOrUpdateMarginAccountAsync(
            marginMode,
            symbol,
            newBalance,
            leverage: null,
            strictValidate: false,
            ct);
    }






    // =================== INTERNAL HTTP ===================

    private async Task<T?> PublicGetAsync<T>(string url, CancellationToken ct)
    {
        using var response = await _publicClient.GetAsync(url,
            HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
    }

    private async Task<T?> AuthGetAsync<T>(string url, CancellationToken ct)
    {
        using var response = await _authClient.GetAsync(url,
            HttpCompletionOption.ResponseHeadersRead, ct);
        await ThrowOnErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
    }

    private async Task<T?> AuthPostFormAsync<T>(
        string url,
        List<KeyValuePair<string, string>> form,
        CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(form);
        using var response = await _authClient.PostAsync(url, content, ct);
        await ThrowOnErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
    }

    private async Task<T?> AuthPatchFormAsync<T>(
        string url,
        List<KeyValuePair<string, string>> form,
        CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(form);
        using var msg = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        return await SendAuthAsync<T>(msg, ct);
    }

    private async Task<T?> AuthPutFormAsync<T>(
        string url,
        List<KeyValuePair<string, string>> form,
        CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(form);

        using var response = await _authClient.PutAsync(url, content, ct);
        await ThrowOnErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
    }

    private async Task<T?> AuthDeleteAsync<T>(string url, CancellationToken ct)
    {
        using var response = await _authClient.DeleteAsync(url, ct);
        await ThrowOnErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
    }

    private async Task<T?> SendAuthAsync<T>(HttpRequestMessage msg, CancellationToken ct)
    {
        using var response = await _authClient.SendAsync(msg, ct);
        await ThrowOnErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
    }

    private async Task ThrowOnErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            body = string.Empty;
        }

        throw new HitBtcApiException(
            $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
            (int)response.StatusCode,
            body);
    }

    // =================== FORM BUILDERS ===================

    // =================== FORM BUILDERS ===================

    private static List<KeyValuePair<string, string>> BuildSpotOrderForm(
        CreateSpotOrderRequest r)
    {
        var list = new List<KeyValuePair<string, string>>(16)
        {
            new("symbol", r.Symbol),
            new("side", r.Side == OrderSide.Buy ? "buy" : "sell"),
            new("quantity", r.Quantity.ToApiString())
        };

        if (r.ClientOrderId is not null)
            list.Add(new("client_order_id", r.ClientOrderId));

        var typeStr = OrderTypeToString(r.Type);
        if (typeStr != "limit")
            list.Add(new("type", typeStr));

        var tifStr = TimeInForceToString(r.TimeInForce);
        list.Add(new("time_in_force", tifStr));

        if (r.Price.HasValue)
            list.Add(new("price", r.Price.Value.ToApiString()));
        if (r.StopPrice.HasValue)
            list.Add(new("stop_price", r.StopPrice.Value.ToApiString()));
        if (r.ExpireTime.HasValue)
            list.Add(new("expire_time", r.ExpireTime.Value.ToString("O")));
        if (r.StrictValidate)
            list.Add(new("strict_validate", "true"));
        if (r.PostOnly)
            list.Add(new("post_only", "true"));
        if (r.DisplayQuantity.HasValue)
            list.Add(new("display_quantity", r.DisplayQuantity.Value.ToApiString()));
        if (r.TakeRate.HasValue)
            list.Add(new("take_rate", r.TakeRate.Value.ToApiString()));
        if (r.MakeRate.HasValue)
            list.Add(new("make_rate", r.MakeRate.Value.ToApiString()));

        return list;
    }

    private static List<KeyValuePair<string, string>> BuildFuturesOrderForm(
        CreateFuturesOrderRequest r)
    {
        var list = new List<KeyValuePair<string, string>>(16)
        {
            new("symbol", r.Symbol),
            new("side", r.Side == OrderSide.Buy ? "buy" : "sell"),
            new("quantity", r.Quantity.ToApiString())
        };

        if (r.ClientOrderId is not null)
            list.Add(new("client_order_id", r.ClientOrderId));

        var typeStr = OrderTypeToString(r.Type);
        if (typeStr != "limit")
            list.Add(new("type", typeStr));

        list.Add(new("time_in_force", TimeInForceToString(r.TimeInForce)));

        if (r.Price.HasValue)
            list.Add(new("price", r.Price.Value.ToApiString()));
        if (r.StopPrice.HasValue)
            list.Add(new("stop_price", r.StopPrice.Value.ToApiString()));
        if (r.ExpireTime.HasValue)
            list.Add(new("expire_time", r.ExpireTime.Value.ToString("O")));
        if (r.StrictValidate)
            list.Add(new("strict_validate", "true"));
        if (r.PostOnly)
            list.Add(new("post_only", "true"));
        if (r.ReduceOnly)
            list.Add(new("reduce_only", "true"));
        if (r.ClosePosition)
            list.Add(new("close_position", "true"));
        if (r.DisplayQuantity.HasValue)
            list.Add(new("display_quantity", r.DisplayQuantity.Value.ToApiString()));

        var marginStr = r.MarginMode == MarginMode.Cross ? "cross" : "isolated";
        list.Add(new("margin_mode", marginStr));

        return list;
    }

    private static string OrderTypeToString(OrderType t) => t switch
    {
        OrderType.Limit => "limit",
        OrderType.Market => "market",
        OrderType.StopLimit => "stopLimit",
        OrderType.StopMarket => "stopMarket",
        OrderType.TakeProfitLimit => "takeProfitLimit",
        OrderType.TakeProfitMarket => "takeProfitMarket",
        _ => "limit"
    };

    private static string TimeInForceToString(TimeInForce tif) => tif switch
    {
        TimeInForce.GTC => "GTC",
        TimeInForce.IOC => "IOC",
        TimeInForce.FOK => "FOK",
        TimeInForce.Day => "Day",
        TimeInForce.GTD => "GTD",
        _ => "GTC"
    };

    public async ValueTask DisposeAsync()
    {
        _publicClient.Dispose();
        _authClient.Dispose();
        await Task.CompletedTask;
    }
}

/// <summary>
/// Исключение при ошибках HitBTC API.
/// </summary>
public sealed class HitBtcApiException : Exception
{
    public int StatusCode { get; }
    public string ResponseBody { get; }

    public HitBtcApiException(string message, int statusCode, string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}