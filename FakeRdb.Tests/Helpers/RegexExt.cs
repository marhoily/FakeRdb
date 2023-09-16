using System.Text.RegularExpressions;

namespace FakeRdb.Tests;

public static class RegexExt
{
    public static Match? FindMatch(this IEnumerable<string> patterns, string input)
    {
        return patterns
            .Select(pattern => Regex.Match(input, pattern))
            .FirstOrDefault(m => m.Success);
    }
}