using HaxcelApp.Core;
using Xunit;

namespace HaxcelApp.Tests;

public class HaxcelTests
{
    private static string Calculate(string expression)
    {
        var table = new TableData();
        table.SetExpression("A1", expression);
        return table.GetValueText("A1");
    }

    [Fact]
    public void Operations_give_correct_values()
    {
        Assert.Equal("7", Calculate("1 + 2 * 3"));
        Assert.Equal("9", Calculate("(1 + 2) * 3"));
        Assert.Equal("-2", Calculate("-5 + +3"));
        Assert.Equal("-3", Calculate("-7 / 2"));
        Assert.Equal("-4", Calculate("-7 div 2"));
        Assert.Equal("2", Calculate("-7 mod 3"));
        Assert.Equal("5", Calculate("max(2, 5)"));
        Assert.Equal("2", Calculate("min(2, 5)"));
        Assert.Equal("1234567890123456789012345678900", Calculate("123456789012345678901234567890 * 10"));
    }

    [Fact]
    public void Errors_are_found()
    {
        var table = new TableData();
        table.SetExpression("A1", "1 / 0");
        table.SetExpression("B1", "A1 + 1");
        table.SetExpression("C1", "D1");
        table.SetExpression("D1", "C1");
        table.SetExpression("E1", "2 + * 3");
        table.SetExpression("F1", "G1 + 1");
        table.SetExpression("H1", "Z99 + 1");

        Assert.Contains("Ділення на нуль", table.GetError("A1"));
        Assert.NotNull(table.GetError("B1"));
        Assert.Contains("Цикліч", table.GetError("C1"));
        Assert.Contains("Цикліч", table.GetError("D1"));
        Assert.Contains("позиція 5", table.GetError("E1"));
        Assert.Contains("порожню клітинку", table.GetError("F1"));
        Assert.Contains("неіснуючу", table.GetError("H1"));
    }

    [Fact]
    public void Table_is_saved_and_loaded_and_recalculated()
    {
        string path = Path.Combine(Path.GetTempPath(), "haxcel_test_" + Guid.NewGuid() + ".json");
        try
        {
            var table = new TableData();
            table.SetExpression("A1", "6");
            table.SetExpression("B1", "A1 * 7");
            table.Save(path);

            var loaded = TableData.Load(path);
            Assert.Equal("42", loaded.GetValueText("B1"));

            loaded.SetExpression("A1", "7");
            Assert.Equal("49", loaded.GetValueText("B1"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
