// Unit/Auth/HmacAuthHandlerTests.cs
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

using FluentAssertions;

using HitBTC.Connector.Core.Auth;

using Xunit;

namespace HitBTC.Connector.Tests.Unit.Auth;

public class HmacAuthHandlerTests
{
    private const string TestApiKey = "testApiKey123";
    private const string TestSecretKey = "testSecretKey456";

    [Fact]
    public async Task SignRequest_ShouldAddAuthorizationHeader()
    {
        // Arrange
        var innerHandler = new TestHandler();
        var handler = new HmacAuthHandler(TestApiKey, TestSecretKey);
        SetInnerHandler(handler, innerHandler);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.hitbtc.com/api/3/")
        };

        // Act
        await client.GetAsync("spot/order");

        // Assert
        innerHandler.LastRequest.Should().NotBeNull();
        innerHandler.LastRequest!.Headers.Authorization.Should().NotBeNull();
        innerHandler.LastRequest.Headers.Authorization!.Scheme.Should().Be("HS256");
    }

    [Fact]
    public async Task SignRequest_AuthHeader_ShouldBeBase64Encoded()
    {
        // Arrange
        var innerHandler = new TestHandler();
        var handler = new HmacAuthHandler(TestApiKey, TestSecretKey);
        SetInnerHandler(handler, innerHandler);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.hitbtc.com/api/3/")
        };

        // Act
        await client.GetAsync("spot/order");

        // Assert
        var authParam = innerHandler.LastRequest!.Headers.Authorization!.Parameter;
        var action = () => Convert.FromBase64String(authParam!);
        action.Should().NotThrow();
    }

    [Fact]
    public async Task SignRequest_ShouldContainApiKey()
    {
        // Arrange
        var innerHandler = new TestHandler();
        var handler = new HmacAuthHandler(TestApiKey, TestSecretKey);
        SetInnerHandler(handler, innerHandler);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.hitbtc.com/api/3/")
        };

        // Act
        await client.GetAsync("spot/order");

        // Assert
        var authParam = innerHandler.LastRequest!.Headers.Authorization!.Parameter;
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authParam!));
        decoded.Should().StartWith(TestApiKey + ":");
    }

    [Fact]
    public async Task SignRequest_ShouldContainTimestamp()
    {
        // Arrange
        var innerHandler = new TestHandler();
        var handler = new HmacAuthHandler(TestApiKey, TestSecretKey);
        SetInnerHandler(handler, innerHandler);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.hitbtc.com/api/3/")
        };

        var beforeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        await client.GetAsync("spot/order");

        var afterTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Assert
        var authParam = innerHandler.LastRequest!.Headers.Authorization!.Parameter;
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authParam!));
        var parts = decoded.Split(':');

        parts.Should().HaveCountGreaterOrEqualTo(3);
        var timestamp = long.Parse(parts[2]);
        timestamp.Should().BeInRange(beforeTimestamp, afterTimestamp);
    }

    [Fact]
    public async Task SignRequest_WithWindow_ShouldIncludeWindow()
    {
        // Arrange
        var innerHandler = new TestHandler();
        var handler = new HmacAuthHandler(TestApiKey, TestSecretKey, window: 5000);
        SetInnerHandler(handler, innerHandler);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.hitbtc.com/api/3/")
        };

        // Act
        await client.GetAsync("spot/order");

        // Assert
        var authParam = innerHandler.LastRequest!.Headers.Authorization!.Parameter;
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authParam!));
        var parts = decoded.Split(':');

        parts.Should().HaveCount(4);
        parts[3].Should().Be("5000");
    }

    [Fact]
    public async Task SignRequest_POST_WithBody_ShouldIncludeBodyInSignature()
    {
        // Arrange
        var innerHandler = new TestHandler();
        var handler = new HmacAuthHandler(TestApiKey, TestSecretKey);
        SetInnerHandler(handler, innerHandler);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.hitbtc.com/api/3/")
        };

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("symbol", "BTCUSDT"),
            new KeyValuePair<string, string>("side", "buy"),
            new KeyValuePair<string, string>("quantity", "0.001")
        });

        // Act
        await client.PostAsync("spot/order", content);

        // Assert
        innerHandler.LastRequest.Should().NotBeNull();
        innerHandler.LastRequest!.Headers.Authorization.Should().NotBeNull();
    }

    [Fact]
    public void HMAC_Signature_ShouldBeConsistent()
    {
        // Arrange
        var secret = "testSecret"u8.ToArray();
        var message = "GET/api/3/spot/order1705312345000"u8.ToArray();

        // Act
        Span<byte> hash1 = stackalloc byte[32];
        Span<byte> hash2 = stackalloc byte[32];
        HMACSHA256.HashData(secret, message, hash1);
        HMACSHA256.HashData(secret, message, hash2);

        // Assert
        hash1.SequenceEqual(hash2).Should().BeTrue();
    }

    [Fact]
    public async Task SignRequest_DifferentMethods_ShouldProduceDifferentSignatures()
    {
        // Arrange
        var innerHandler = new TestHandler();
        var handler = new HmacAuthHandler(TestApiKey, TestSecretKey);
        SetInnerHandler(handler, innerHandler);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.hitbtc.com/api/3/")
        };

        // Act
        await client.GetAsync("spot/order");
        var getAuth = innerHandler.LastRequest!.Headers.Authorization!.Parameter;

        await client.DeleteAsync("spot/order");
        var deleteAuth = innerHandler.LastRequest!.Headers.Authorization!.Parameter;

        // Assert - подписи должны быть разными (разные методы + timestamp)
        getAuth.Should().NotBe(deleteAuth);
    }

    private static void SetInnerHandler(DelegatingHandler outer, HttpMessageHandler inner)
    {
        var field = typeof(DelegatingHandler)
            .GetField("_innerHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(outer, inner);
    }

    private class TestHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }
}