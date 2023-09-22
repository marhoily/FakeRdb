using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace FakeRdb;

public static class ParserRuleContextExtensions
{
    public static string DumpTree(this ParserRuleContext context, Parser parser)
    {
        var sb = new StringBuilder();
        DumpTree(context, parser, sb, string.Empty);
        return sb.ToString();
    }

    private static void DumpTree(IParseTree node, Parser parser, StringBuilder sb, string indent)
    {
        if (node is ParserRuleContext context)
        {
            // Rule context, print the rule name
            var ruleName = parser.RuleNames[context.RuleIndex];
            sb.AppendLine($"{indent}{ruleName}");
        }
        else if (node is TerminalNodeImpl terminal)
        {
            // Terminal node, print the token
            var tokenName = terminal.Symbol.Type >= 0 ? parser.Vocabulary.GetDisplayName(terminal.Symbol.Type) : terminal.Symbol.Text;
            sb.AppendLine($"{indent}{tokenName}");
        }
        else
        {
            // Unknown type of node, should not happen
            sb.AppendLine($"{indent}???");
        }

        // Recurse into child nodes with an increased indent
        for (int i = 0; i < node.ChildCount; ++i)
        {
            DumpTree(node.GetChild(i), parser, sb, indent + "  ");
        }
    }
}