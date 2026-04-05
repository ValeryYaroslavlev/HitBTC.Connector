// Core/Infrastructure/BufferPool.cs
using System.Buffers;
using System.Runtime.CompilerServices;

namespace HitBTC.Connector.Core.Infrastructure;

/// <summary>
/// Обёртка над ArrayPool для удобного управления буферами.
/// </summary>
public static class BufferPool
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] RentBytes(int minimumLength)
        => ArrayPool<byte>.Shared.Rent(minimumLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnBytes(byte[] buffer)
        => ArrayPool<byte>.Shared.Return(buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char[] RentChars(int minimumLength)
        => ArrayPool<char>.Shared.Rent(minimumLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnChars(char[] buffer)
        => ArrayPool<char>.Shared.Return(buffer);
}