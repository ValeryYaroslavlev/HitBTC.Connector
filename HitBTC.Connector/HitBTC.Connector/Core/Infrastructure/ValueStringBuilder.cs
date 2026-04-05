// Core/Infrastructure/ValueStringBuilder.cs
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HitBTC.Connector.Core.Infrastructure;

/// <summary>
/// Stack-allocated string builder для zero-allocation конкатенации.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public ref struct ValueStringBuilder
{
    private Span<char> _buffer;
    private int _position;

    public ValueStringBuilder(Span<char> initialBuffer)
    {
        _buffer = initialBuffer;
        _position = 0;
    }

    public readonly int Length => _position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ReadOnlySpan<char> value)
    {
        if (_position + value.Length > _buffer.Length)
            throw new InvalidOperationException("ValueStringBuilder buffer overflow");

        value.CopyTo(_buffer[_position..]);
        _position += value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char value)
    {
        if (_position >= _buffer.Length)
            throw new InvalidOperationException("ValueStringBuilder buffer overflow");

        _buffer[_position++] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string value) => Append(value.AsSpan());

    public readonly ReadOnlySpan<char> AsSpan() => _buffer[.._position];

    public readonly override string ToString() => new(_buffer[.._position]);
}