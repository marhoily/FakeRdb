namespace FakeRdb.Utils;

public static class PrettyPrint
{
    public static string Table(List<string> headers,
        List<List<object?>> rows)
    {
        if (headers.Count != 0 && rows.Count == 0)
        {
            return string.Join(", ", headers);
        }
        var widths = Enumerable.Range(0, headers.Count)
            .Select(i => Math.Max(
                headers[i].Length,
                rows.Select(row => PrintObj(row[i]).Length).Max()))
            .ToArray();

        var h = headers.Select((header, i) => header.PadRight(widths[i]));
        var header = "│ " + string.Join(" │ ", h) + " │";
        var top = Border("┌─┬┐");
        var bottom = Border("└─┴┘");
        var separator = Border("├─┼┤");
        var dataRows = rows.Select(row => "│ " + string.Join(" │ ", RowData(row)) + " │");

        return string.Join(Environment.NewLine,
            new[] { top, header, separator }
                .Concat(dataRows).Append(bottom));

        string Border(string map) => map[0] + string.Join(map[2], widths.Select(width => new string(map[1], width + 2))) + map[3];
        IEnumerable<string> RowData(IEnumerable<object?> row) =>
            row.Select((data, i) => PrintObj(data).PadRight(widths[i]));

        static string PrintObj(object? data)
        {
            return data switch
            {
                null => "<NULL>",
                double d=> d.ToString("F"),
                _ => data.ToString()!
            };
        }
    }
}