using Antlr4.Runtime;

namespace FakeRdb;

public class PanicErrorListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        throw new InvalidOperationException($"line {line}:{charPositionInLine} {msg}", e);
    }

}