// Fixtures/TestConfiguration.cs
using Microsoft.Extensions.Configuration;

namespace HitBTC.Connector.Tests.Fixtures;

public static class TestConfiguration
{
    private static readonly IConfiguration _configuration;

    static TestConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddEnvironmentVariables("HITBTC_TEST_")
            .Build();
    }

    public static string ApiKey =>
        _configuration["HitBTC:ApiKey"] ??
        Environment.GetEnvironmentVariable("HITBTC_TEST_API_KEY") ??
        string.Empty;

    public static string SecretKey =>
        _configuration["HitBTC:SecretKey"] ??
        Environment.GetEnvironmentVariable("HITBTC_TEST_SECRET_KEY") ??
        string.Empty;

    public static bool RunIntegrationTests =>
        bool.TryParse(_configuration["TestSettings:RunIntegrationTests"], out var run) && run;

    public static bool RunStressTests =>
        bool.TryParse(_configuration["TestSettings:RunStressTests"], out var run) && run;

    public static string TestSymbol =>
        _configuration["TestSettings:TestSymbol"] ?? "BTCUSDT";

    public static string TestFuturesSymbol =>
        _configuration["TestSettings:TestFuturesSymbol"] ?? "BTCUSDT_PERP";

    public static bool HasCredentials =>
        !string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(SecretKey);
}