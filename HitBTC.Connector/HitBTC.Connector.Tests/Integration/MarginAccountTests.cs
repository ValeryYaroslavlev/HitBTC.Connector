// Integration/MarginAccountTests.cs
using FluentAssertions;

using HitBTC.Connector.Core.Models;
using HitBTC.Connector.Rest;
using HitBTC.Connector.Tests.Fixtures;

using Xunit;

namespace HitBTC.Connector.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Integration")]
public class MarginAccountTests : IAsyncLifetime
{
    private HitBtcRestClient _client = null!;
    private const string TestFuturesSymbol = "BTCUSDT_PERP";

    public Task InitializeAsync()
    {
        if (!TestConfiguration.HasCredentials)
        {
            return Task.CompletedTask;
        }

        _client = new HitBtcRestClient(
            TestConfiguration.ApiKey,
            TestConfiguration.SecretKey);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateMarginAccount_WithLeverage_ShouldWork()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests && TestConfiguration.HasCredentials,
            "Integration tests disabled or no credentials");

        FuturesMarginAccount? account = null;
        try
        {
            // Act - Create with small balance
            account = await _client.CreateOrUpdateMarginAccountAsync(
                MarginMode.Isolated,
                TestFuturesSymbol,
                marginBalance: 1m,  // Минимум для теста
                leverage: 10m);

            // Assert
            account.Should().NotBeNull();
            account.Symbol.Should().Be(TestFuturesSymbol);
            account.Leverage.Should().Be(10m);
            account.Currencies.Should().NotBeEmpty();
            account.Currencies[0].MarginBalance.Should().Be(1m);
        }
        finally
        {
            // Cleanup - close account
            if (account is not null)
            {
                await _client.CloseMarginAccountAsync(MarginMode.Isolated, TestFuturesSymbol);
            }
        }
    }

    [Fact]
    public async Task UpdateLeverage_ShouldWork()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests && TestConfiguration.HasCredentials,
            "Integration tests disabled or no credentials");

        try
        {
            // Arrange - create account
            await _client.CreateOrUpdateMarginAccountAsync(
                MarginMode.Isolated,
                TestFuturesSymbol,
                marginBalance: 1m,
                leverage: 10m);

            // Act - update leverage
            var updated = await _client.UpdateLeverageAsync(TestFuturesSymbol, 25m);

            // Assert
            updated.Leverage.Should().Be(25m);
        }
        finally
        {
            await _client.CloseMarginAccountAsync(MarginMode.Isolated, TestFuturesSymbol);
        }
    }

    [Fact]
    public async Task AddAndRemoveMargin_ShouldWork()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests && TestConfiguration.HasCredentials,
            "Integration tests disabled or no credentials");

        try
        {
            // Create
            var account = await _client.CreateOrUpdateMarginAccountAsync(
                MarginMode.Isolated,
                TestFuturesSymbol,
                marginBalance: 5m,
                leverage: 10m);

            var initialBalance = account.Currencies[0].MarginBalance;

            // Add margin
            var afterAdd = await _client.AddMarginAsync(
                MarginMode.Isolated,
                TestFuturesSymbol,
                amount: 2m);

            afterAdd.Currencies[0].MarginBalance.Should().Be(initialBalance + 2m);

            // Remove margin
            var afterRemove = await _client.RemoveMarginAsync(
                MarginMode.Isolated,
                TestFuturesSymbol,
                amount: 1m);

            afterRemove.Currencies[0].MarginBalance.Should().Be(initialBalance + 1m);
        }
        finally
        {
            await _client.CloseMarginAccountAsync(MarginMode.Isolated, TestFuturesSymbol);
        }
    }

    [Fact]
    public async Task CloseMarginAccount_ShouldReturnFundsToTradingAccount()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests && TestConfiguration.HasCredentials,
            "Integration tests disabled or no credentials");

        // Arrange
        var beforeBalances = await _client.GetFuturesBalanceAsync();
        var beforeUsdtAvailable = beforeBalances
            .FirstOrDefault(b => b.Currency == "USDT")?.Available ?? 0m;

        // Create account
        await _client.CreateOrUpdateMarginAccountAsync(
            MarginMode.Isolated,
            TestFuturesSymbol,
            marginBalance: 1m,
            leverage: 10m);

        // Act - Close
        var closedAccount = await _client.CloseMarginAccountAsync(
            MarginMode.Isolated,
            TestFuturesSymbol);

        // Assert
        closedAccount.Currencies[0].MarginBalance.Should().Be(0m);

        // Check funds returned
        var afterBalances = await _client.GetFuturesBalanceAsync();
        var afterUsdtAvailable = afterBalances
            .FirstOrDefault(b => b.Currency == "USDT")?.Available ?? 0m;

        afterUsdtAvailable.Should().BeGreaterThanOrEqualTo(beforeUsdtAvailable);
    }
}