using static FakeRdb.IR;
using System.Text;

namespace FakeRdb;

public static class DebugPrint
{
    public static string Print(this IExpression exp)
    {
        var builder = new StringBuilder();
        Inner(exp, builder, false);
        return builder.ToString();

        static void Inner(IExpression exp, StringBuilder builder, bool needParens)
        {
            switch (exp)
            {
                case BinaryExp binary:
                    bool thisNeedParens = needParens && binary.Op == BinaryOperator.Or;
                    if (thisNeedParens) builder.Append('(');
                    Inner(binary.Left, builder, binary.Op == BinaryOperator.And);
                    builder.Append($" {BinaryOperatorToString(binary.Op)} ");
                    Inner(binary.Right, builder, binary.Op == BinaryOperator.And);
                    if (thisNeedParens) builder.Append(')');
                    break;
                case UnaryExp unaryExp:
                    builder.Append(UnaryOperatorToString(unaryExp.Op));
                    Inner(unaryExp.Operand, builder, false);
                    break;
                case ColumnExp columnExp:
                    builder.Append(columnExp.FullColumnName);
                    break;
                case LiteralExp literalExp:
                    builder.Append($"'{literalExp.Value}'");
                    break;
                case BindExp bindExp:
                    builder.Append(bindExp.Value);
                    break;
                case AggregateExp aggregateExp:
                    // For demonstration, not complete
                    builder.Append($"{aggregateExp.Function.Method.Name}(");
                    // Print arguments here...
                    builder.Append(')');
                    break;
                case ScalarExp scalarExp:
                    // For demonstration, not complete
                    builder.Append($"{scalarExp.Function.Method.Name}(");
                    // Print arguments here...
                    builder.Append(')');
                    break;
                case InExp inExp:
                    Inner(inExp.Needle, builder, false);
                    builder.Append($" IN {inExp.Haystack}");
                    break;
                default:
                    throw new Exception($"Unknown expression type: {exp.GetType().Name}");
            }
        }
        static string BinaryOperatorToString(BinaryOperator op)
        {
            return op switch
            {
                BinaryOperator.And => "&&",
                BinaryOperator.Or => "||",
                BinaryOperator.Equal => "==",
                _ => throw new Exception($"Unknown BinaryOperator: {op}")
            };
        }
        static string UnaryOperatorToString(UnaryOperator op)
        {
            return op switch
            {
                UnaryOperator.Not => "!",
                _ => throw new Exception($"Unknown UnaryOperator: {op}")
            };
        }
    }

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