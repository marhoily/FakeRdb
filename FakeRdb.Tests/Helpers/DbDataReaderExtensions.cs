﻿using Xunit.Abstractions;

namespace FakeRdb.Tests;

public static class DbDataReaderExtensions
{
    public static void ShouldEqual(this DbDataReader actual, DbDataReader expected, ITestOutputHelper outputHelper)
    {
        actual.IsClosed.Should().BeFalse();
        expected.IsClosed.Should().BeFalse();
        expected.RecordsAffected.Should().Be(actual.RecordsAffected);
        var expectedSchema = expected.GetSchema().ToList();
        actual.GetSchema().Should().BeEquivalentTo(
            expectedSchema, opt => opt.WithStrictOrdering());
        var expectedData = expected.ReadData();
        outputHelper.PrintOut(expectedSchema
            .Select(c => c.ColumnName)
            .ToList(), expectedData);
        actual.ReadData().Should().BeEquivalentTo(expectedData,
            opt => opt
                .WithStrictOrdering()
                .Using<double>(ctx => ctx.Subject.Should()
                    .BeApproximately(ctx.Expectation, 1e-4))
                .WhenTypeIs<double>()
                .Using<float>(ctx => ctx.Subject.Should()
                    .BeApproximately(ctx.Expectation, 1e-4f))
                .WhenTypeIs<float>());
    }

    private static void PrintOut(this ITestOutputHelper output,
        List<string> headers,
        List<List<object?>> rows)
    {
        var widths = Enumerable.Range(0, headers.Count)
            .Select(i => Math.Max(
                headers[i].Length,
                rows.Select(row => row[i]?.ToString()?.Length ?? 0).Max()))
            .ToArray();

        var h = headers.Select((header, i) => header.PadRight(widths[i]));
        var header = "│ " + string.Join(" │ ", h) + " │";
        var top = Border("┌─┬┐");
        var bottom = Border("└─┴┘");
        var separator = Border("├─┼┤");      
        var dataRows = rows.Select(row => "│ " + string.Join(" │ ", RowData(row)) + " │");

        output.WriteLine(string.Join(Environment.NewLine,
            new[] { top, header, separator}
                .Concat(dataRows).Append(bottom)));
        return;

        string Border(string map) => map[0] + string.Join(map[2], widths.Select(width => new string(map[1], width + 2))) + map[3];
        IEnumerable<string> RowData(IEnumerable<object?> row) =>
            row.Select((data, i) =>
                (data?.ToString() ?? "").PadRight(widths[i]));
    }

    private static IEnumerable<(string, string ColumnName)> GetSchema(this DbDataReader reader)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            yield return (reader.GetDataTypeName(i), reader.GetName(i));
        }
    }

    private static List<List<object?>> ReadData(this DbDataReader reader)
    {
        var rows = new List<List<object?>>();
        while (reader.Read())
        {
            var row = new List<object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetDataTypeName(i) switch
                {
                    "NUMERIC" => reader.GetDecimal(i),
                    _ => reader.GetValue(i),
                };
                row.Add(value == DBNull.Value ? null : value);
            }
            rows.Add(row);
        }
        return rows;
    }

}