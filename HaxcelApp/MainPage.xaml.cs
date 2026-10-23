using HaxcelApp.Core;

namespace HaxcelApp;

public partial class MainPage : ContentPage
{
    const int CountColumn = 10;
    const int CountRow = 20;

    TableData table = new TableData(CountRow, CountColumn);
    Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
    string selectedAddress = "A1";
    bool showExpressions = false;
    bool updating = false;

    Color normalColor = Colors.White;
    Color errorColor = Color.FromArgb("#FFD6D6");

    public MainPage()
    {
        InitializeComponent();
        CreateGrid();
    }

    private void CreateGrid()
    {
        updating = true;
        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();
        entries.Clear();

        AddColumnsAndColumnLabels();
        AddRowsAndCellEntries();

        updating = false;
        RefreshCells();
    }

    private void AddColumnsAndColumnLabels()
    {
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(40)));

        for (int col = 0; col < table.ColumnCount; col++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(90)));
            grid.Add(MakeLabel(TableData.GetColumnName(col + 1)), col + 1, 0);
        }
    }

    private void AddRowsAndCellEntries()
    {
        for (int row = 0; row < table.RowCount; row++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.Add(MakeLabel((row + 1).ToString()), 0, row + 1);

            for (int col = 0; col < table.ColumnCount; col++)
            {
                string address = TableData.MakeAddress(col, row);
                var entry = new Entry
                {
                    ClassId = address,
                    Text = "",
                    BackgroundColor = normalColor,
                    TextColor = Colors.Black
                };
                entry.Focused += Entry_Focused;
                entry.Unfocused += Entry_Unfocused;

                grid.Add(entry, col + 1, row + 1);
                entries[address] = entry;
            }
        }
    }

    private Label MakeLabel(string text)
    {
        return new Label
        {
            Text = text,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };
    }

    private string GetDisplayText(string address)
    {
        if (showExpressions)
        {
            return table.GetExpression(address);
        }
        if (table.GetError(address) != null)
        {
            return "#ПОМИЛКА";
        }
        return table.GetValueText(address);
    }

    private void RefreshCells()
    {
        updating = true;
        foreach (var pair in entries)
        {
            var entry = pair.Value;
            if (entry.IsFocused)
            {
                continue;
            }

            entry.Text = GetDisplayText(pair.Key);
            if (table.GetError(pair.Key) != null)
            {
                entry.BackgroundColor = errorColor;
            }
            else
            {
                entry.BackgroundColor = normalColor;
            }
        }
        updating = false;
    }

    private void ShowCellStatus(string address)
    {
        statusLabel.Text = table.GetError(address) ?? "Готово";
    }

    private void Entry_Focused(object sender, FocusEventArgs e)
    {
        var entry = (Entry)sender;
        selectedAddress = entry.ClassId;
        addressLabel.Text = selectedAddress;

        updating = true;
        entry.Text = table.GetExpression(selectedAddress);
        textInput.Text = entry.Text;
        updating = false;

        ShowCellStatus(selectedAddress);
    }

    private void Entry_Unfocused(object sender, FocusEventArgs e)
    {
        if (updating)
        {
            return;
        }

        var entry = (Entry)sender;
        string address = entry.ClassId;
        string text = entry.Text ?? "";

        if (text != table.GetExpression(address))
        {
            table.SetExpression(address, text);
        }
        RefreshCells();
        ShowCellStatus(address);
    }

    private void TextInput_Completed(object sender, EventArgs e)
    {
        table.SetExpression(selectedAddress, textInput.Text ?? "");
        RefreshCells();
        ShowCellStatus(selectedAddress);
    }

    private async void NewButton_Clicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlertAsync("Нова таблиця", "Поточна таблиця буде втрачена. Продовжити?", "Так", "Ні");
        if (answer)
        {
            table = new TableData(CountRow, CountColumn);
            CreateGrid();
            statusLabel.Text = "Створено нову таблицю";
        }
    }

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string path = Path.Combine(folder, "table.json");
            table.Save(path);
            await DisplayAlertAsync("Збереження", "Таблицю збережено у файл:\n" + path, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Помилка збереження", ex.Message, "OK");
        }
    }

    private async void ReadButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync();
            if (file == null)
            {
                return;
            }

            table = TableData.Load(file.FullPath);
            CreateGrid();
            statusLabel.Text = "Прочитано: " + file.FullPath;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Помилка читання", ex.Message, "OK");
        }
    }

    private async void ExitButton_Clicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlertAsync("Підтвердження", "Ви дійсно хочете вийти?", "Так", "Ні");
        if (answer)
        {
            Application.Current?.Quit();
        }
    }

    private void CalculateButton_Clicked(object sender, EventArgs e)
    {
        table.Recalculate();
        RefreshCells();
        statusLabel.Text = "Таблицю перераховано";
    }

    private void ModeButton_Clicked(object sender, EventArgs e)
    {
        showExpressions = !showExpressions;
        if (showExpressions)
        {
            ModeButton.Text = "Режим: ВИРАЗ";
        }
        else
        {
            ModeButton.Text = "Режим: ЗНАЧЕННЯ";
        }
        RefreshCells();
    }

    private void AddRowButton_Clicked(object sender, EventArgs e)
    {
        if (table.AddRow())
        {
            CreateGrid();
        }
    }

    private void AddColumnButton_Clicked(object sender, EventArgs e)
    {
        if (table.AddColumn())
        {
            CreateGrid();
        }
    }

    private async void DeleteRowButton_Clicked(object sender, EventArgs e)
    {
        if (!table.IsLastRowEmpty())
        {
            bool answer = await DisplayAlertAsync("Підтвердження", "Останній рядок не порожній. Видалити його?", "Так", "Ні");
            if (!answer)
            {
                return;
            }
        }
        if (table.RemoveRow())
        {
            CreateGrid();
        }
    }

    private async void DeleteColumnButton_Clicked(object sender, EventArgs e)
    {
        if (!table.IsLastColumnEmpty())
        {
            bool answer = await DisplayAlertAsync("Підтвердження", "Останній стовпчик не порожній. Видалити його?", "Так", "Ні");
            if (!answer)
            {
                return;
            }
        }
        if (table.RemoveColumn())
        {
            CreateGrid();
        }
    }

    private async void HelpButton_Clicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Довідка",
            "Клікніть на клітинку, введіть вираз і перейдіть в іншу клітинку (або натисніть Enter у верхньому полі).\n\n" +
            "У виразі: цілі числа, адреси клітинок (A1), дужки, операції + - * / mod div, унарні + -, max(x, y), min(x, y).\n\n" +
            "Помилки (клітинка червона, текст внизу): синтаксична з позицією, ділення на нуль, циклічне посилання.\n\n" +
            "Кнопка «Режим» перемикає показ значень і виразів.",
            "Закрити");
    }

    private async void AboutButton_Clicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Про програму",
            "Електронна таблиця. Лабораторна робота №1, варіант 3.\n" +
            "Операції: + - * /, mod, div, унарні + -, max, min.\n" +
            "Виконувала студентка групи К-28 Харченко Поліна",
            "OK");
    }
}
