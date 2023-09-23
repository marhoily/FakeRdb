namespace FakeRdb;

/// <summary>
/// Represents a store for managing hierarchical name-value pairs of type <typeparamref name="T"/>. 
/// The values associated with names are only available once the relevant scope is closed. 
/// Scopes must be closed in the same order they are opened to maintain data integrity.
/// </summary>
/// <typeparam name="T">The type of value to store.</typeparam>
public sealed class HierarchicalAliasStore<T>
{
    private readonly Dictionary<string, T> _permanent = new();
    private readonly Stack<Dictionary<string, T>> _temp = new();

    /// <summary>
    /// Opens a new scope for adding name-value pairs.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> representing the opened scope.</returns>
    public IDisposable OpenScope()
    {
        var tmp = new Dictionary<string, T>();
        _temp.Push(tmp);
        return new Scope(this, tmp);
    }

    private void CloseScope(Dictionary<string, T> temp)
    {
        if (_temp.Pop() != temp)
            throw new InvalidOperationException(
                "A scope was attempted to be closed out of order in HierarchicalAliasStore. " +
                "Scopes must be closed in the same order they are opened to maintain data integrity. " +
                "Please review the scope management to ensure proper nesting and closing of scopes."
            );
        foreach (var pair in temp)
            _permanent.Add(pair.Key, pair.Value);
    }

    private readonly struct Scope : IDisposable
    {
        private readonly HierarchicalAliasStore<T> _owner;
        private readonly Dictionary<string, T> _temp;

        public Scope(
            HierarchicalAliasStore<T> owner,
            Dictionary<string, T> temp)
        {
            _owner = owner;
            _temp = temp;
        }

        /// <summary>
        /// Closes the current scope and makes its names available in the <see cref="HierarchicalAliasStore{T}"/>.
        /// </summary>
        public void Dispose()
        {
            _owner.CloseScope(_temp);
        }
    }

    /// <summary>
    /// Associates the specified name with the specified value in the current scope.
    /// </summary>
    /// <param name="value">The value to set.</param>
    /// <param name="name">The name with which the specified value is to be associated.</param>
    public void Set(T value, string name)
    {
        _temp.Peek()[name] = value;
    }

    /// <summary>
    /// Tries to get the value associated with the specified name.
    /// </summary>
    /// <param name="name">The name whose value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified name, if the name is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns>true if the <see cref="HierarchicalAliasStore{T}"/> contains an element with the specified name; otherwise, false.</returns>
    public bool TryGet(string name, out T value)
    {
        return _permanent.TryGetValue(name, out value!);
    }
}
