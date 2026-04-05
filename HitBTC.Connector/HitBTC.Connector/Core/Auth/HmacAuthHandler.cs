// Core/Auth/HmacAuthHandler.cs
using System.Buffers;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace HitBTC.Connector.Core.Auth;

public sealed class HmacAuthHandler : DelegatingHandler
{
    private readonly string _apiKey;
    private readonly byte[] _secretKeyBytes;
    private readonly int? _window;

    public HmacAuthHandler(string apiKey, string secretKey, int? window = null)
        : base(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 20,
            EnableMultipleHttp2Connections = true,
            UseProxy = false,
            UseCookies = false
        })
    {
        _apiKey = apiKey;
        _secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        _window = window;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await SignRequestAsync(request);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task SignRequestAsync(HttpRequestMessage request)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var windowStr = _window?.ToString();

        var sb = new StringBuilder(512);

        // Method
        sb.Append(request.Method.Method);

        // Path - используем AbsolutePath (включает /api/3/)
        var uri = request.RequestUri!;
        sb.Append(uri.AbsolutePath);

        // Query
        if (!string.IsNullOrEmpty(uri.Query))
        {
            sb.Append(uri.Query);
        }

        // Body
        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsStringAsync();
            sb.Append(body);
        }

        // Timestamp
        sb.Append(timestamp);

        // Window
        if (windowStr is not null)
        {
            sb.Append(windowStr);
        }

        var messageBytes = Encoding.UTF8.GetBytes(sb.ToString());

        // HMAC-SHA256
        Span<byte> hash = stackalloc byte[32];
        HMACSHA256.HashData(_secretKeyBytes, messageBytes, hash);

        var signature = Convert.ToHexStringLower(hash);

        // Header
        var headerPayloadBuilder = new StringBuilder(256);
        headerPayloadBuilder.Append(_apiKey);
        headerPayloadBuilder.Append(':');
        headerPayloadBuilder.Append(signature);
        headerPayloadBuilder.Append(':');
        headerPayloadBuilder.Append(timestamp);

        if (windowStr is not null)
        {
            headerPayloadBuilder.Append(':');
            headerPayloadBuilder.Append(windowStr);
        }

        var headerPayload = headerPayloadBuilder.ToString();
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerPayload));

        request.Headers.Authorization = new AuthenticationHeaderValue("HS256", base64);
    }
}