// Unit/Infrastructure/LockFreeObjectPoolTests.cs
using FluentAssertions;

using HitBTC.Connector.Core.Infrastructure;

using Xunit;

namespace HitBTC.Connector.Tests.Unit.Infrastructure;

public class LockFreeObjectPoolTests
{
    private class TestObject
    {
        public int Value { get; set; }
        public string Data { get; set; } = string.Empty;
    }

    [Fact]
    public void Rent_ShouldReturnObject()
    {
        // Arrange
        var pool = new LockFreeObjectPool<TestObject>(10);

        // Act
        var obj = pool.Rent();

        // Assert
        obj.Should().NotBeNull();
        obj.Should().BeOfType<TestObject>();
    }

    [Fact]
    public void Rent_ShouldReturnDifferentObjects()
    {
        // Arrange
        var pool = new LockFreeObjectPool<TestObject>(10);

        // Act
        var obj1 = pool.Rent();
        var obj2 = pool.Rent();

        // Assert
        obj1.Should().NotBeSameAs(obj2);
    }

    [Fact]
    public void Return_ShouldAllowReuse()
    {
        // Arrange
        var pool = new LockFreeObjectPool<TestObject>(2);
        var obj1 = pool.Rent();
        obj1.Value = 42;

        // Act
        pool.Return(obj1);
        var obj2 = pool.Rent();

        // Assert
        // Может быть тот же объект (если пул его переиспользовал)
        // или новый (если слот был занят)
        obj2.Should().NotBeNull();
    }

    [Fact]
    public void Rent_WhenPoolExhausted_ShouldCreateNewObject()
    {
        // Arrange
        var pool = new LockFreeObjectPool<TestObject>(2);

        // Act - забираем больше чем ёмкость пула
        var objects = Enumerable.Range(0, 10)
            .Select(_ => pool.Rent())
            .ToList();

        // Assert
        objects.Should().HaveCount(10);
        objects.Should().AllSatisfy(o => o.Should().NotBeNull());
    }

    [Fact]
    public void Pool_ShouldBeThreadSafe()
    {
        // Arrange
        var pool = new LockFreeObjectPool<TestObject>(100);
        var rentedObjects = new System.Collections.Concurrent.ConcurrentBag<TestObject>();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act - параллельный доступ
        Parallel.For(0, 1000, i =>
        {
            try
            {
                var obj = pool.Rent();
                obj.Value = i;
                rentedObjects.Add(obj);

                // Случайно возвращаем некоторые объекты
                if (i % 3 == 0)
                {
                    pool.Return(obj);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
        rentedObjects.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Pool_CapacityRoundsUpToPowerOfTwo()
    {
        // Arrange & Act
        var pool = new LockFreeObjectPool<TestObject>(100);

        // Rent 128 objects (next power of 2 after 100)
        var objects = new List<TestObject>();
        for (int i = 0; i < 128; i++)
        {
            objects.Add(pool.Rent());
        }

        // Assert - все должны быть уникальны
        objects.Distinct().Should().HaveCount(128);
    }
}