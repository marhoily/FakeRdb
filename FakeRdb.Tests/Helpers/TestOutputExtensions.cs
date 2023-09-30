namespace FakeRdb.Tests;

public static class TestOutputExtensions
{
    public static void Print(this ITestOutputHelper output, ReaderResult value)
    {
        output.PrintTable(value.Schema
            .Select(c => c.ColumnName + ": " + c.ColumnType)
            .ToList(), value.Data);
    }

    public static void PrintTable(this ITestOutputHelper output,
        List<string> headers,
        List<List<object?>> rows)
    {
        if (headers.Count != 0 && rows.Count == 0)
        {
            output.WriteLine(string.Join(", ", headers));
            return;
        }
        var widths = Enumerable.Range(0, headers.Count)
            .Select(i => Math.Max(
                headers[i].Length,
                rows.Select(row => PrintObj(row[i]).Length).Max()))
            .ToArray();

        var h = headers.Select((header, i) => header.PadRight(widths[i]));
        var header = "│ " + string.Join(" │ ", h) + " │";
        var top = Border("┌─┬┐", widths);
        var bottom = Border("└─┴┘", widths);
        var separator = Border("├─┼┤", widths);
        var dataRows = rows.Select(row => "│ " + string.Join(" │ ", RowData(row, widths)) + " │");

        output.WriteLine(string.Join(Environment.NewLine,
            new[] { top, header, separator }
                .Concat(dataRows).Append(bottom)));
    }

    private static IEnumerable<string> RowData(IEnumerable<object?> row, int[] widths) 
        => row.Select((data, i) => PrintObj(data).PadRight(widths[i]));

    private static string PrintObj(object? data)
    {
        return data switch
        {
            null => "<NULL>",
            double d=> d.ToString("F"),
            _ => data.ToString()!
        };
    }

    private static string Border(string map, int[] widths) => 
        map[0] + string.Join(map[2], 
                   widths.Select(width => new string(map[1], width + 2)))
               + map[3];
}