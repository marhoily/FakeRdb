namespace FakeRdb.Tests;

public sealed class HierarchicalAliasStoreTests
{
    [Fact]
    public void OpenScope_ShouldCreateNewScope()
    {
        // Arrange
        var store = new HierarchicalAliasStore<string>();

        // Act
        using (store.OpenScope())
        {
            // Act inside the scope
            store.Set("value", "name");

            // Assert inside the scope
            store.TryGet("name", out _).Should().BeFalse();
        }

        // Assert
        store.TryGet("name", out var valueAfterScope).Should().BeTrue();
        valueAfterScope.Should().Be("value");
    }

    [Fact]
    public void CloseScope_OutOfOrder_ShouldThrowException()
    {
        // Arrange
        var store = new HierarchicalAliasStore<string>();
        var scope1 = store.OpenScope();
        store.OpenScope();

        // Act & Assert
        Action act = () => scope1.Dispose();
        act.Should().Throw<InvalidOperationException>().WithMessage("*closed out of order*");
    }

    [Fact]
    public void Set_ShouldSetValueInCurrentScope()
    {
        // Arrange
        var store = new HierarchicalAliasStore<string>();
        
        // Act
        using (store.OpenScope())
        {
            store.Set("value", "name");
        }

        // Assert
        store.TryGet("name", out var value).Should().BeTrue();
        value.Should().Be("value");
    }

    [Fact]
    public void TryGet_ShouldRetrieveSetValue()
    {
        // Arrange
        var store = new HierarchicalAliasStore<string>();
        using (store.OpenScope())
        {
            store.Set("value", "name");
        }

        // Act
        var retrieved = store.TryGet("name", out var value);

        // Assert
        retrieved.Should().BeTrue();
        value.Should().Be("value");
    }

    [Fact]
    public void TryGet_WithoutSet_ShouldReturnFalse()
    {
        // Arrange
        var store = new HierarchicalAliasStore<string>();

        // Act
        var retrieved = store.TryGet("name", out var value);

        // Assert
        retrieved.Should().BeFalse();
        value.Should().BeNull();
    }
}