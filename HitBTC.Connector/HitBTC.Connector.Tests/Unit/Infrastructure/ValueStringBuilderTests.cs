// Unit/Infrastructure/ValueStringBuilderTests.cs
using FluentAssertions;

using HitBTC.Connector.Core.Infrastructure;

using Xunit;

namespace HitBTC.Connector.Tests.Unit.Infrastructure;

public class ValueStringBuilderTests
{
    [Fact]
    public void Append_String_ShouldWork()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        var sb = new ValueStringBuilder(buffer);

        // Act
        sb.Append("Hello");
        sb.Append(" ");
        sb.Append("World");

        // Assert
        sb.ToString().Should().Be("Hello World");
        sb.Length.Should().Be(11);
    }

    [Fact]
    public void Append_Char_ShouldWork()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        var sb = new ValueStringBuilder(buffer);

        // Act
        sb.Append('A');
        sb.Append('B');
        sb.Append('C');

        // Assert
        sb.ToString().Should().Be("ABC");
    }

    [Fact]
    public void Append_ReadOnlySpan_ShouldWork()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        var sb = new ValueStringBuilder(buffer);
        ReadOnlySpan<char> text = "Test".AsSpan();

        // Act
        sb.Append(text);

        // Assert
        sb.ToString().Should().Be("Test");
    }

    [Fact]
    public void Append_WhenBufferFull_ShouldThrow()
    {
        // Arrange & Act & Assert
        // Используем отдельный метод, так как ref struct нельзя в лямбде
        var exception = CaptureOverflowException();

        exception.Should().NotBeNull();
        exception.Should().BeOfType<InvalidOperationException>();
        exception!.Message.Should().Contain("overflow");
    }

    // Отдельный метод для захвата исключения (ref struct нельзя в лямбде)
    private static Exception? CaptureOverflowException()
    {
        try
        {
            Span<char> buffer = stackalloc char[5];
            var sb = new ValueStringBuilder(buffer);

            sb.Append("Hello"); // Exactly 5 chars - OK
            sb.Append("X");     // Overflow - должно бросить исключение

            return null; // Если дошли сюда - исключения не было
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Fact]
    public void Append_CharOverflow_ShouldThrow()
    {
        // Arrange & Act & Assert
        var exception = CaptureCharOverflowException();

        exception.Should().NotBeNull();
        exception.Should().BeOfType<InvalidOperationException>();
    }

    private static Exception? CaptureCharOverflowException()
    {
        try
        {
            Span<char> buffer = stackalloc char[3];
            var sb = new ValueStringBuilder(buffer);

            sb.Append('A');
            sb.Append('B');
            sb.Append('C'); // Заполнили буфер
            sb.Append('D'); // Overflow

            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Fact]
    public void AsSpan_ShouldReturnCorrectSpan()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        var sb = new ValueStringBuilder(buffer);
        sb.Append("Test123");

        // Act
        var span = sb.AsSpan();

        // Assert
        span.Length.Should().Be(7);
        new string(span).Should().Be("Test123");
    }

    [Fact]
    public void BuildUrlPath_RealWorldScenario()
    {
        // Arrange
        Span<char> buffer = stackalloc char[256];
        var sb = new ValueStringBuilder(buffer);
        var symbol = "BTCUSDT";
        var depth = 100;

        // Act
        sb.Append("public/orderbook/");
        sb.Append(symbol);
        sb.Append("?depth=");
        sb.Append(depth.ToString());

        // Assert
        sb.ToString().Should().Be("public/orderbook/BTCUSDT?depth=100");
    }

    [Fact]
    public void EmptyBuilder_ShouldReturnEmptyString()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        var sb = new ValueStringBuilder(buffer);

        // Assert
        sb.Length.Should().Be(0);
        sb.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Append_EmptyString_ShouldNotChangeLength()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        var sb = new ValueStringBuilder(buffer);
        sb.Append("Test");

        // Act
        sb.Append(string.Empty);
        sb.Append(ReadOnlySpan<char>.Empty);

        // Assert
        sb.Length.Should().Be(4);
        sb.ToString().Should().Be("Test");
    }

    [Fact]
    public void MultipleAppends_ShouldConcatenateCorrectly()
    {
        // Arrange & Act
        var result = BuildComplexUrl("BTCUSDT", 100, "ASC");

        // Assert
        result.Should().Be("public/candles/BTCUSDT?depth=100&sort=ASC");
    }

    // Helper метод для построения сложного URL
    private static string BuildComplexUrl(string symbol, int depth, string sort)
    {
        Span<char> buffer = stackalloc char[256];
        var sb = new ValueStringBuilder(buffer);

        sb.Append("public/candles/");
        sb.Append(symbol);
        sb.Append("?depth=");
        sb.Append(depth.ToString());
        sb.Append("&sort=");
        sb.Append(sort);

        return sb.ToString();
    }

    [Fact]
    public void ExactFit_ShouldWork()
    {
        // Arrange - буфер точно под размер данных
        var data = "12345";
        Span<char> buffer = stackalloc char[5];
        var sb = new ValueStringBuilder(buffer);

        // Act
        sb.Append(data);

        // Assert
        sb.Length.Should().Be(5);
        sb.ToString().Should().Be("12345");
    }

    [Fact]
    public void SingleChar_ShouldWork()
    {
        // Arrange
        Span<char> buffer = stackalloc char[1];
        var sb = new ValueStringBuilder(buffer);

        // Act
        sb.Append('X');

        // Assert
        sb.Length.Should().Be(1);
        sb.ToString().Should().Be("X");
    }

    [Theory]
    [InlineData("Hello", " ", "World", "Hello World")]
    [InlineData("A", "B", "C", "ABC")]
    [InlineData("", "Test", "", "Test")]
    [InlineData("123", "456", "789", "123456789")]
    public void Append_VariousStrings_ShouldWork(string a, string b, string c, string expected)
    {
        // Arrange & Act
        var result = ConcatenateStrings(a, b, c);

        // Assert
        result.Should().Be(expected);
    }

    private static string ConcatenateStrings(string a, string b, string c)
    {
        Span<char> buffer = stackalloc char[256];
        var sb = new ValueStringBuilder(buffer);

        sb.Append(a);
        sb.Append(b);
        sb.Append(c);

        return sb.ToString();
    }
}