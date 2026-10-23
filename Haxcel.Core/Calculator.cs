using System.Numerics;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace HaxcelApp.Core;

public static class Calculator
{
    public static IParseTree Parse(string expression)
    {
        var listener = new ThrowExceptionErrorListener();

        var lexer = new LabCalculatorLexer(new AntlrInputStream(expression));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);

        var tokens = new CommonTokenStream(lexer);
        var parser = new LabCalculatorParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        return parser.compileUnit();
    }

    public static BigInteger Calculate(string expression, Func<string, BigInteger> getCellValue)
    {
        var tree = Parse(expression);
        var visitor = new Visitor(getCellValue);
        return visitor.Visit(tree);
    }
}
