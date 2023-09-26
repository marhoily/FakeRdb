using System.Diagnostics.CodeAnalysis;

namespace FakeRdb;

public static class Assertions
{
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static void AssertAreAllEqual<T>(this IEnumerable<T> src)
    {
        var first = true;
        var current = default(T);
        foreach (var item in src) {
            if (first)
                first = false;
            else if (!Equals(current, item))
                throw new Exception(
                    "Assertion Failed: Not all elements are equal: " + 
                    string.Join(", ", src.Select(elem => elem?.ToString() ?? "<NULL>")));
            current = item;
        }
    }
}