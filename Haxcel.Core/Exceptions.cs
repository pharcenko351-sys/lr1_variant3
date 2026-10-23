namespace HaxcelApp.Core;

public class ExpressionException : Exception
{
    public int Position { get; }

    public ExpressionException(string message, int position) : base(message)
    {
        Position = position;
    }
}

public class CalculationException : Exception
{
    public CalculationException(string message) : base(message)
    {
    }
}
