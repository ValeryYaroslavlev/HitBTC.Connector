// WebSocket/HitBtcWebSocketClient.cs
using System.Buffers;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

using HitBTC.Connector.Core.Infrastructure;
using HitBTC.Connector.Core.Models;

namespace HitBTC.Connector.WebSocket;

/// <summary>
/// High-performance WebSocket клиент с lock-free message pipeline.
/// 
/// Архитектура:
///   ReceiveLoop -> IncomingChannel -> ProcessLoop -> Events
///   User calls -> OutgoingChannel -> SendLoop -> WS
/// </summary>
public sealed class HitBtcWebSocketClient : IHitBtcWebSocketClient
{
    private static readonly string[] Endpoints =
    {
        "wss://api.hitbtc.com/api/3/ws/public",
        "wss://api.hitbtc.com/api/3/ws/trading",
        "wss://api.hitbtc.com/api/3/ws/wallet"
    };

    private readonly string _endpoint;
    private readonly string? _apiKey;
    private readonly byte[]? _secretKeyBytes;
    private readonly WebSocketEndpoint _endpointType;
    private readonly JsonSerializerOptions _jsonOptions;

    // WebSocket
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;

    // Lock-free channels
    private readonly Channel<byte[]> _outgoing;
    private readonly Channel<byte[]> _incoming;

    // Heartbeat
    private readonly TimeSpan _pingInterval = TimeSpan.FromSeconds(15);

    // Events
    public event Action<string, OrderBookSnapshot>? OrderBookSnapshotReceived;
    public event Action<string, OrderBookSnapshot>? OrderBookUpdateReceived;
    public event Action<string, PublicTrade[]>? TradesReceived;
    public event Action<SpotOrder>? SpotOrderUpdated;
    public event Action<FuturesOrder>? FuturesOrderUpdated;
    public event Action<string>? RawMessageReceived;
    public event Action<Exception>? ErrorOccurred;
    public event Action? Connected;
    public event Action<string>? Disconnected;

    private int _isConnected;
    public bool IsConnected => Volatile.Read(ref _isConnected) == 1;

    public HitBtcWebSocketClient(
        WebSocketEndpoint endpoint,
        string? apiKey = null,
        string? secretKey = null)
    {
        _endpointType = endpoint;
        _endpoint = Endpoints[(int)endpoint];
        _apiKey = apiKey;
        _secretKeyBytes = secretKey is not null
            ? Encoding.UTF8.GetBytes(secretKey)
            : null;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        _outgoing = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _incoming = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ws = new ClientWebSocket();
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

        await _ws.ConnectAsync(new Uri(_endpoint), _cts.Token);
        Interlocked.Exchange(ref _isConnected, 1);

        // Auth
        if (_apiKey is not null && _secretKeyBytes is not null)
        {
            await AuthenticateAsync(_cts.Token);
        }

        // Start background loops
        _ = Task.Factory.StartNew(
            () => ReceiveLoopAsync(_cts.Token),
            TaskCreationOptions.LongRunning);

        _ = Task.Factory.StartNew(
            () => SendLoopAsync(_cts.Token),
            TaskCreationOptions.LongRunning);

        _ = Task.Factory.StartNew(
            () => ProcessLoopAsync(_cts.Token),
            TaskCreationOptions.LongRunning);

        _ = Task.Factory.StartNew(
            () => HeartbeatLoopAsync(_cts.Token),
            TaskCreationOptions.LongRunning);

        Connected?.Invoke();
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_ws is not null && _ws.State == WebSocketState.Open)
        {
            try
            {
                await _ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client disconnect",
                    ct);
            }
            catch { /* ignore */ }
        }

        _cts?.Cancel();
        Interlocked.Exchange(ref _isConnected, 0);
        Disconnected?.Invoke("Client requested disconnect");
    }

    // =================== SUBSCRIPTIONS ===================

    public Task SubscribeOrderBookAsync(string symbol, CancellationToken ct = default)
    {
        var msg = new
        {
            method = "subscribe",
            ch = "orderbook/full",
            @params = new { symbols = new[] { symbol } },
            id = GenerateId()
        };
        return SendJsonAsync(msg, ct);
    }

    public Task SubscribeTradesAsync(string symbol, CancellationToken ct = default)
    {
        var msg = new
        {
            method = "subscribe",
            ch = "trades",
            @params = new { symbols = new[] { symbol } },
            id = GenerateId()
        };
        return SendJsonAsync(msg, ct);
    }

    public Task SubscribeCandlesAsync(string symbol, CandlePeriod period, CancellationToken ct = default)
    {
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

        var msg = new
        {
            method = "subscribe",
            ch = $"candles/{periodStr}",
            @params = new { symbols = new[] { symbol } },
            id = GenerateId()
        };
        return SendJsonAsync(msg, ct);
    }

    public Task UnsubscribeOrderBookAsync(string symbol, CancellationToken ct = default)
    {
        var msg = new
        {
            method = "unsubscribe",
            ch = "orderbook/full",
            @params = new { symbols = new[] { symbol } },
            id = GenerateId()
        };
        return SendJsonAsync(msg, ct);
    }

    public Task UnsubscribeTradesAsync(string symbol, CancellationToken ct = default)
    {
        var msg = new
        {
            method = "unsubscribe",
            ch = "trades",
            @params = new { symbols = new[] { symbol } },
            id = GenerateId()
        };
        return SendJsonAsync(msg, ct);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        await _outgoing.Writer.WriteAsync(data.ToArray(), ct);
    }

    // =================== BACKGROUND LOOPS ===================

    /// <summary>
    /// Приём данных из WebSocket с использованием ArrayPool.
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            while (!ct.IsCancellationRequested && _ws!.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                ValueWebSocketReceiveResult result;

                do
                {
                    result = await _ws.ReceiveAsync(
                        buffer.AsMemory(), ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Interlocked.Exchange(ref _isConnected, 0);
                        Disconnected?.Invoke("Server closed connection");
                        return;
                    }

                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                var messageBytes = ms.ToArray();
                await _incoming.Writer.WriteAsync(messageBytes, ct);
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (WebSocketException ex)
        {
            Interlocked.Exchange(ref _isConnected, 0);
            ErrorOccurred?.Invoke(ex);
            Disconnected?.Invoke(ex.Message);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Отправка данных из outgoing channel в WebSocket.
    /// </summary>
    private async Task SendLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var data in _outgoing.Reader.ReadAllAsync(ct))
            {
                if (_ws?.State != WebSocketState.Open) break;

                await _ws.SendAsync(
                    data.AsMemory(),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
    }

    /// <summary>
    /// Обработка входящих сообщений из incoming channel.
    /// Парсинг JSON с минимальными аллокациями через Utf8JsonReader.
    /// </summary>
    private async Task ProcessLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var data in _incoming.Reader.ReadAllAsync(ct))
            {
                try
                {
                    ProcessMessage(data);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(ex);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Heartbeat - периодическая отправка ping.
    /// </summary>
    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(_pingInterval, ct);

                if (_ws?.State == WebSocketState.Open)
                {
                    var pingMsg = JsonSerializer.SerializeToUtf8Bytes(new
                    {
                        method = "ping"
                    });
                    await _outgoing.Writer.WriteAsync(pingMsg, ct);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    // =================== MESSAGE PROCESSING ===================

    private void ProcessMessage(byte[] data)
    {
        // Уведомляем подписчиков о сыром сообщении
        RawMessageReceived?.Invoke(Encoding.UTF8.GetString(data));

        var reader = new Utf8JsonReader(data);
        using var doc = JsonDocument.Parse(data);
        var root = doc.RootElement;

        // Определяем тип сообщения
        if (root.TryGetProperty("ch", out var chProp))
        {
            var channel = chProp.GetString();
            if (channel is null) return;

            DispatchChannelMessage(channel, root);
            return;
        }

        // Ответ на метод (login, subscribe и т.д.)
        if (root.TryGetProperty("id", out _))
        {
            // Можно обработать результат подписки/логина
            return;
        }
    }

    private void DispatchChannelMessage(string channel, JsonElement root)
    {
        // orderbook/full
        if (channel.StartsWith("orderbook/full", StringComparison.Ordinal))
        {
            if (root.TryGetProperty("snapshot", out var snapshot))
            {
                foreach (var symbolProp in snapshot.EnumerateObject())
                {
                    var ob = JsonSerializer.Deserialize<OrderBookSnapshot>(
                        symbolProp.Value.GetRawText(), _jsonOptions);
                    if (ob is not null)
                        OrderBookSnapshotReceived?.Invoke(symbolProp.Name, ob);
                }
            }

            if (root.TryGetProperty("update", out var update))
            {
                foreach (var symbolProp in update.EnumerateObject())
                {
                    var ob = JsonSerializer.Deserialize<OrderBookSnapshot>(
                        symbolProp.Value.GetRawText(), _jsonOptions);
                    if (ob is not null)
                        OrderBookUpdateReceived?.Invoke(symbolProp.Name, ob);
                }
            }
            return;
        }

        // trades
        if (channel.StartsWith("trades", StringComparison.Ordinal))
        {
            if (root.TryGetProperty("snapshot", out var snapshot) ||
                root.TryGetProperty("update", out snapshot))
            {
                foreach (var symbolProp in snapshot.EnumerateObject())
                {
                    var trades = JsonSerializer.Deserialize<PublicTrade[]>(
                        symbolProp.Value.GetRawText(), _jsonOptions);
                    if (trades is not null)
                        TradesReceived?.Invoke(symbolProp.Name, trades);
                }
            }
            return;
        }

        // spot/order
        if (channel == "spot/order")
        {
            if (root.TryGetProperty("data", out var dataEl))
            {
                var order = JsonSerializer.Deserialize<SpotOrder>(
                    dataEl.GetRawText(), _jsonOptions);
                if (order is not null)
                    SpotOrderUpdated?.Invoke(order);
            }
            return;
        }

        // futures/order
        if (channel == "futures/order")
        {
            if (root.TryGetProperty("data", out var dataEl))
            {
                var order = JsonSerializer.Deserialize<FuturesOrder>(
                    dataEl.GetRawText(), _jsonOptions);
                if (order is not null)
                    FuturesOrderUpdated?.Invoke(order);
            }
        }
    }

    // =================== AUTH ===================

    private async Task AuthenticateAsync(CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = timestamp.ToString();

        Span<byte> hash = stackalloc byte[32];
        HMACSHA256.HashData(
            _secretKeyBytes!,
            Encoding.UTF8.GetBytes(nonce),
            hash);

        var signature = Convert.ToHexStringLower(hash);

        var loginMsg = new
        {
            method = "login",
            @params = new
            {
                algo = "HS256",
                pKey = _apiKey,
                nonce = nonce,
                signature = signature
            },
            id = GenerateId()
        };

        await SendJsonAsync(loginMsg, ct);

        // Ждём ответ на login
        await Task.Delay(500, ct);
    }

    // =================== HELPERS ===================

    private Task SendJsonAsync<T>(T message, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
        return _outgoing.Writer.WriteAsync(bytes, ct).AsTask();
    }

    private static int _idCounter;

    private static int GenerateId()
        => Interlocked.Increment(ref _idCounter);

    public async ValueTask DisposeAsync()
    {
        _outgoing.Writer.TryComplete();
        _incoming.Writer.TryComplete();

        if (_ws is not null)
        {
            if (_ws.State == WebSocketState.Open)
            {
                try
                {
                    await _ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Disposing",
                        CancellationToken.None);
                }
                catch { /* ignore */ }
            }

            _ws.Dispose();
        }

        _cts?.Cancel();
        _cts?.Dispose();
    }
}