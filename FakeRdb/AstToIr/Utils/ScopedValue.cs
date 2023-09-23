namespace FakeRdb;

/// <summary>
/// Manages a scoped value within a certain context. This is particularly useful in
/// <see cref="AstToIrVisitor"/> where different SQL statements or clauses may require 
/// isolated scopes for variables or tables.
/// </summary>
/// <typeparam name="T">Type of the scoped value.</typeparam>
public struct ScopedValue<T> : IDisposable where T : notnull
{
    private T? _value;

    /// <summary>
    /// Gets the current scoped value. Throws an <see cref="InvalidOperationException"/> 
    /// if the value is not set.
    /// </summary>
    public T Value => _value ?? throw new InvalidOperationException("Value is not set in this scope.");

    /// <summary>
    /// Sets a new value for this scope.
    /// </summary>
    /// <param name="value">The value to set.</param>
    /// <returns>Returns the updated ScopedValue structure.</returns>
    public ScopedValue<T> Set(T value)
    {
        _value = value;
        return this;
    }

    /// <summary>
    /// Resets the scoped value to its default when exiting the scope.
    /// </summary>
    public void Dispose()
    {
        _value = default;
    }
}