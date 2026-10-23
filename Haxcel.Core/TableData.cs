using System.Numerics;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace HaxcelApp.Core;

public class TableFile
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public Dictionary<string, string> Cells { get; set; } = new();
}

public class TableData
{
    public const int MaxRows = 50;
    public const int MaxColumns = 26;

    public int RowCount { get; private set; }
    public int ColumnCount { get; private set; }

    private readonly Dictionary<string, string> expressions = new();
    private readonly Dictionary<string, BigInteger> values = new();
    private readonly Dictionary<string, string> errors = new();
    private readonly HashSet<string> visiting = new();

    public TableData(int rows = 10, int columns = 8)
    {
        RowCount = Math.Clamp(rows, 1, MaxRows);
        ColumnCount = Math.Clamp(columns, 1, MaxColumns);
    }

    public void SetExpression(string address, string text)
    {
        address = NormalizeAddress(address);

        if (string.IsNullOrWhiteSpace(text))
        {
            expressions.Remove(address);
        }
        else
        {
            expressions[address] = text;
        }
        Recalculate();
    }

    public string GetExpression(string address)
    {
        expressions.TryGetValue(NormalizeAddress(address), out var text);
        return text ?? "";
    }

    public string GetValueText(string address)
    {
        if (values.TryGetValue(NormalizeAddress(address), out var value))
        {
            return value.ToString();
        }
        return "";
    }

    public string? GetError(string address)
    {
        errors.TryGetValue(NormalizeAddress(address), out var error);
        return error;
    }

    public void Recalculate()
    {
        values.Clear();
        errors.Clear();
        visiting.Clear();

        foreach (var address in expressions.Keys.ToList())
        {
            try
            {
                GetCellValue(address);
            }
            catch (CalculationException)
            {
            }
        }
    }

    public BigInteger GetCellValue(string address)
    {
        address = NormalizeAddress(address);

        if (!IsAddressInRange(address))
        {
            throw new CalculationException("Посилання на неіснуючу клітинку " + address);
        }
        if (!expressions.ContainsKey(address))
        {
            return 0;
        }
        if (errors.ContainsKey(address))
        {
            throw new CalculationException("Клітинка " + address + " містить помилку: " + errors[address]);
        }
        if (values.TryGetValue(address, out var ready))
        {
            return ready;
        }
        if (!visiting.Add(address))
        {
            throw new CalculationException("Циклічне посилання на клітинку " + address);
        }

        try
        {
            var result = Calculator.Calculate(expressions[address], GetCellValue);
            values[address] = result;
            return result;
        }
        catch (ExpressionException ex)
        {
            string message = "Синтаксична помилка (позиція " + (ex.Position + 1) + "): " + ex.Message;
            errors[address] = message;
            throw new CalculationException(message);
        }
        catch (CalculationException ex)
        {
            errors[address] = ex.Message;
            throw;
        }
        finally
        {
            visiting.Remove(address);
        }
    }

    public bool AddRow()
    {
        if (RowCount >= MaxRows)
        {
            return false;
        }
        RowCount++;
        return true;
    }

    public bool AddColumn()
    {
        if (ColumnCount >= MaxColumns)
        {
            return false;
        }
        ColumnCount++;
        return true;
    }

    public bool RemoveRow()
    {
        if (RowCount <= 1)
        {
            return false;
        }

        foreach (var address in expressions.Keys.ToList())
        {
            TryParseAddress(address, out int col, out int row);
            if (row == RowCount - 1)
            {
                expressions.Remove(address);
            }
        }
        RowCount--;
        Recalculate();
        return true;
    }

    public bool RemoveColumn()
    {
        if (ColumnCount <= 1)
        {
            return false;
        }

        foreach (var address in expressions.Keys.ToList())
        {
            TryParseAddress(address, out int col, out int row);
            if (col == ColumnCount - 1)
            {
                expressions.Remove(address);
            }
        }
        ColumnCount--;
        Recalculate();
        return true;
    }

    public bool IsLastRowEmpty()
    {
        foreach (var address in expressions.Keys)
        {
            TryParseAddress(address, out int col, out int row);
            if (row == RowCount - 1)
            {
                return false;
            }
        }
        return true;
    }

    public bool IsLastColumnEmpty()
    {
        foreach (var address in expressions.Keys)
        {
            TryParseAddress(address, out int col, out int row);
            if (col == ColumnCount - 1)
            {
                return false;
            }
        }
        return true;
    }

    public void Save(string path)
    {
        var file = new TableFile
        {
            Rows = RowCount,
            Columns = ColumnCount,
            Cells = new Dictionary<string, string>(expressions)
        };
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        File.WriteAllText(path, JsonSerializer.Serialize(file, options));
    }

    public static TableData Load(string path)
    {
        var file = JsonSerializer.Deserialize<TableFile>(File.ReadAllText(path));
        if (file == null)
        {
            throw new InvalidDataException("Файл має неправильний формат");
        }

        var table = new TableData(file.Rows, file.Columns);
        foreach (var pair in file.Cells)
        {
            string address = NormalizeAddress(pair.Key);
            if (table.IsAddressInRange(address) && !string.IsNullOrWhiteSpace(pair.Value))
            {
                table.expressions[address] = pair.Value;
            }
        }
        table.Recalculate();
        return table;
    }

    public static string GetColumnName(int colIndex)
    {
        int dividend = colIndex;
        string columnName = string.Empty;

        while (dividend > 0)
        {
            int modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }
        return columnName;
    }

    public static string MakeAddress(int col, int row)
    {
        return GetColumnName(col + 1) + (row + 1);
    }

    public static bool TryParseAddress(string address, out int col, out int row)
    {
        col = -1;
        row = -1;

        int i = 0;
        int letters = 0;
        while (i < address.Length && char.IsAsciiLetter(address[i]))
        {
            letters = letters * 26 + (char.ToUpperInvariant(address[i]) - 'A' + 1);
            i++;
        }
        if (i == 0 || i == address.Length)
        {
            return false;
        }

        if (!int.TryParse(address.Substring(i), System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out int number) || number < 1)
        {
            return false;
        }

        col = letters - 1;
        row = number - 1;
        return true;
    }

    private static string NormalizeAddress(string address)
    {
        return address.Trim().ToUpperInvariant();
    }

    public bool IsAddressInRange(string address)
    {
        return TryParseAddress(address, out int col, out int row) && col < ColumnCount && row < RowCount;
    }
}
