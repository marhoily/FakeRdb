namespace FakeRdb.Tests;

public class RowTests
{
    private static readonly Row Row1 = new(new object?[] { 1, "Apple", null });
    private static readonly Row Row2 = new(new object?[] { 2, "Banana", null });
    private static readonly Row RowNulls = new(new object?[] { null, null, null });
    private static readonly Row RowMixed = new(new object?[] { 1.0, "Apple", 1 });

    [Fact]
    public void GetKey_ShouldReturnEqualKeysForSameData()
    {
        var key1 = Row1.GetKey(new[] { 0, 1, 2 });
        var key2 = Row1.GetKey(new[] { 0, 1, 2 });

        key1.Should().BeEquivalentTo(key2);
        key1.GetHashCode().Should().Be(key2.GetHashCode());
    }

    [Fact]
    public void GetKey_ShouldReturnDifferentKeysForDifferentData()
    {
        var key1 = Row1.GetKey(new[] { 0, 1, 2 });
        var key2 = Row2.GetKey(new[] { 0, 1, 2 });

        key1.Should().NotBeEquivalentTo(key2);
        key1.GetHashCode().Should().NotBe(key2.GetHashCode());
    }

    [Fact]
    public void GetKey_ShouldReturnSameKeyForUnitColumn()
    {
        var key1 = Row1.GetKey(Array.Empty<int>());
        var key2 = Row2.GetKey(Array.Empty<int>());

        key1.Should().BeEquivalentTo(key2);
        key1.GetHashCode().Should().Be(key2.GetHashCode());
    }

    [Fact]
    public void GetKey_ShouldHandleAllNullValues()
    {
        var key1 = RowNulls.GetKey(new[] { 0, 1, 2 });
        var key2 = RowNulls.GetKey(new[] { 0, 1, 2 });

        key1.Should().BeEquivalentTo(key2);
        key1.GetHashCode().Should().Be(key2.GetHashCode());
    }

    [Fact]
    public void GetKey_ShouldHandleMixedTypes()
    {
        var key1 = Row1.GetKey(new[] { 0, 1 });
        var key2 = RowMixed.GetKey(new[] { 0, 1 });

        key1.Should().NotBeEquivalentTo(key2);
        key1.GetHashCode().Should().NotBe(key2.GetHashCode());
    }
}