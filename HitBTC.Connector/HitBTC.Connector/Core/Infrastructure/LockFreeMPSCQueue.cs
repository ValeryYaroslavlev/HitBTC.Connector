// Core/Infrastructure/LockFreeMPSCQueue.cs
using System.Runtime.CompilerServices;

namespace HitBTC.Connector.Core.Infrastructure;

/// <summary>
/// Lock-free Multi-Producer Single-Consumer queue.
/// Node — reference type, чтобы Volatile.Read/Write и Interlocked работали корректно.
/// </summary>
public sealed class LockFreeMPSCQueue<T> where T : class
{
    private sealed class Node
    {
        public T? Value;
        public Node? Next;
    }

    private Node _head;
    private Node _tail;

    public LockFreeMPSCQueue()
    {
        var stub = new Node();
        _head = stub;
        _tail = stub;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(T item)
    {
        var node = new Node { Value = item };
        var prev = Interlocked.Exchange(ref _head, node);
        Volatile.Write(ref prev.Next, node);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue(out T? result)
    {
        var tail = _tail;
        var next = Volatile.Read(ref tail.Next);

        if (next is null)
        {
            result = default;
            return false;
        }

        result = next.Value;
        next.Value = default; // allow GC
        _tail = next;
        return true;
    }
}