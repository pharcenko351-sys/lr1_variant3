using Antlr4.Runtime;

namespace HaxcelApp.Core;

public class ThrowExceptionErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new ExpressionException("неочікувана лексема (" + msg + ")", charPositionInLine);
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new ExpressionException("недопустимий символ (" + msg + ")", charPositionInLine);
    }
}
