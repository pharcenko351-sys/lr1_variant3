using System.Numerics;
using Antlr4.Runtime.Misc;

namespace HaxcelApp.Core;

public class Visitor : LabCalculatorBaseVisitor<BigInteger>
{
    private readonly Func<string, BigInteger> getCellValue;

    public Visitor(Func<string, BigInteger> getCellValue)
    {
        this.getCellValue = getCellValue;
    }

    public override BigInteger VisitCompileUnit([NotNull] LabCalculatorParser.CompileUnitContext context)
    {
        return Visit(context.expression());
    }

    public override BigInteger VisitNumberExpr([NotNull] LabCalculatorParser.NumberExprContext context)
    {
        return BigInteger.Parse(context.GetText());
    }

    public override BigInteger VisitIdentifierExpr([NotNull] LabCalculatorParser.IdentifierExprContext context)
    {
        return getCellValue(context.GetText());
    }

    public override BigInteger VisitParenthesizedExpr([NotNull] LabCalculatorParser.ParenthesizedExprContext context)
    {
        return Visit(context.expression());
    }

    public override BigInteger VisitUnaryExpr([NotNull] LabCalculatorParser.UnaryExprContext context)
    {
        var value = Visit(context.expression());

        if (context.operatorToken.Type == LabCalculatorLexer.SUBTRACT)
        {
            return -value;
        }
        return value;
    }

    public override BigInteger VisitAdditiveExpr([NotNull] LabCalculatorParser.AdditiveExprContext context)
    {
        var left = WalkLeft(context);
        var right = WalkRight(context);

        if (context.operatorToken.Type == LabCalculatorLexer.ADD)
        {
            return left + right;
        }
        return left - right;
    }

    public override BigInteger VisitMultiplicativeExpr([NotNull] LabCalculatorParser.MultiplicativeExprContext context)
    {
        var left = WalkLeft(context);
        var right = WalkRight(context);
        int type = context.operatorToken.Type;

        if (type == LabCalculatorLexer.MULTIPLY)
        {
            return left * right;
        }

        if (right.IsZero)
        {
            throw new CalculationException("Ділення на нуль (позиція " + (context.operatorToken.Column + 1) + ")");
        }

        if (type == LabCalculatorLexer.DIVIDE)
        {
            return BigInteger.Divide(left, right);
        }

        if (type == LabCalculatorLexer.DIV)
        {
            var quotient = BigInteger.DivRem(left, right, out var remainder);
            if (!remainder.IsZero && (remainder.Sign < 0) != (right.Sign < 0))
            {
                quotient = quotient - 1;
            }
            return quotient;
        }

        var rest = BigInteger.Remainder(left, right);
        if (!rest.IsZero && (rest.Sign < 0) != (right.Sign < 0))
        {
            rest = rest + right;
        }
        return rest;
    }

    public override BigInteger VisitFunctionExpr([NotNull] LabCalculatorParser.FunctionExprContext context)
    {
        var first = Visit(context.expression(0));
        var second = Visit(context.expression(1));

        if (context.funcName.Type == LabCalculatorLexer.MAX)
        {
            return BigInteger.Max(first, second);
        }
        return BigInteger.Min(first, second);
    }

    private BigInteger WalkLeft(LabCalculatorParser.ExpressionContext context)
    {
        return Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(0));
    }

    private BigInteger WalkRight(LabCalculatorParser.ExpressionContext context)
    {
        return Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(1));
    }
}
