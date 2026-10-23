using System.Numerics;
using HaxcelApp.Core;
using Xunit;

namespace HaxcelApp.Tests;

public class HaxcelTests
{
    private static TableData MakeTable(params (string address, string text)[] cells)
    {
        var table = new TableData();
        foreach (var cell in cells)
        {
            table.SetExpression(cell.address, cell.text);
        }
        return table;
    }

    [Theory]
    [InlineData("1+2*3", "7")]
    [InlineData("(1+2)*3", "9")]
    [InlineData("10-4-3", "3")]
    [InlineData("-5 + +3", "-2")]
    [InlineData("2 * -3", "-6")]
    [InlineData("-7 / 2", "-3")]
    [InlineData("-7 div 2", "-4")]
    [InlineData("-7 mod 3", "2")]
    [InlineData("7 mod -3", "-2")]
    [InlineData("max(2, 5)", "5")]
    [InlineData("min(max(1, 9), 4)", "4")]
    [InlineData("123456789012345678901234567890 * 10", "1234567890123456789012345678900")]
    public void Expression_gives_correct_value(string expression, string expected)
    {
        var table = MakeTable(("A1", expression));
        Assert.Equal(expected, table.GetValueText("A1"));
    }

    [Fact]
    public void Dependent_cells_are_recalculated()
    {
        var table = MakeTable(("A1", "5"), ("B1", "A1 * 2"));
        table.SetExpression("A1", "7");
        Assert.Equal("14", table.GetValueText("B1"));
    }

    [Theory]
    [InlineData("1/0")]
    [InlineData("5 div 0")]
    [InlineData("5 mod (3-3)")]
    public void Division_by_zero_gives_error(string expression)
    {
        var table = MakeTable(("A1", expression));
        Assert.Contains("Ділення на нуль", table.GetError("A1"));
    }

    [Fact]
    public void Cycle_is_found()
    {
        var table = MakeTable(("A1", "B1 + 1"), ("B1", "A1 + 1"));
        Assert.Contains("Цикліч", table.GetError("A1"));
        Assert.Contains("Цикліч", table.GetError("B1"));
    }

    [Fact]
    public void Error_goes_to_dependent_cells()
    {
        var table = MakeTable(("A1", "1/0"), ("B1", "A1 + 1"));
        Assert.NotNull(table.GetError("B1"));
    }

    [Theory]
    [InlineData("2 + * 3", 4)]
    [InlineData("(1 + 2", 6)]
    [InlineData("3 $ 4", 2)]
    public void Syntax_error_has_position(string expression, int position)
    {
        var error = Assert.Throws<ExpressionException>(() => Calculator.Parse(expression));
        Assert.Equal(position, error.Position);
    }

    [Fact]
    public void Remove_row_deletes_its_cells()
    {
        var table = MakeTable(("A10", "1"));
        Assert.False(table.IsLastRowEmpty());
        table.RemoveRow();
        Assert.Equal(9, table.RowCount);
    }

    [Fact]
    public void Save_and_load_gives_same_table()
    {
        string path = Path.Combine(Path.GetTempPath(), "lab1_test_" + Guid.NewGuid() + ".json");
        try
        {
            MakeTable(("A1", "6"), ("B1", "A1 * 7")).Save(path);
            Assert.Equal("42", TableData.Load(path).GetValueText("B1"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Test_files_are_read_correctly()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "TestData");

        var valid = TableData.Load(Path.Combine(folder, "valid.json"));
        Assert.Equal("2", valid.GetValueText("C1"));
        Assert.Equal("23", valid.GetValueText("B3"));

        var big = TableData.Load(Path.Combine(folder, "bigint.json"));
        Assert.Equal(BigInteger.Pow(10, 33).ToString(), big.GetValueText("C1"));

        var errors = TableData.Load(Path.Combine(folder, "errors.json"));
        Assert.Contains("Синтаксична", errors.GetError("A1"));
        Assert.Contains("Ділення на нуль", errors.GetError("A2"));
        Assert.Contains("Цикліч", errors.GetError("A3"));
    }
}
