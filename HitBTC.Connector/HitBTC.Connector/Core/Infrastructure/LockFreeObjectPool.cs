// Core/Infrastructure/LockFreeObjectPool.cs
using System.Numerics;
using System.Runtime.CompilerServices;

namespace HitBTC.Connector.Core.Infrastructure;

/// <summary>
/// Lock-free object pool. Элементы переиспользуются без блокировок.
/// T должен быть class с конструктором без параметров.
/// </summary>
public sealed class LockFreeObjectPool<T> where T : class, new()
{
    private readonly T?[] _items;
    private readonly int _capacity;

    public LockFreeObjectPool(int capacity = 256)
    {
        _capacity = (int)BitOperations.RoundUpToPowerOf2((uint)capacity);
        _items = new T?[_capacity];

        for (int i = 0; i < _capacity; i++)
        {
            _items[i] = new T();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Rent()
    {
        for (int i = 0; i < _capacity; i++)
        {
            var item = Interlocked.Exchange(ref _items[i], null);
            if (item is not null)
                return item;
        }

        return new T();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(T item)
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (Interlocked.CompareExchange(ref _items[i], item, null) is null)
                return;
        }
        // Pool full — item is dropped (GC will collect)
    }
}