using CommunityToolkit.Maui.Storage;
using HaxcelApp.Core;

namespace HaxcelApp;

public partial class MainPage : ContentPage
{
    const int CountColumn = 10;
    const int CountRow = 10;
    const double CellWidth = 90;
    const double CellHeight = 36;

    TableData table = new TableData(CountRow, CountColumn);
    Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
    List<Label> columnLabels = new List<Label>();
    List<Label> rowLabels = new List<Label>();
    int builtRows = 0;
    int builtColumns = 0;

    string selectedAddress = "A1";
    bool showExpressions = false;
    bool updating = false;
    bool closeConfirmed = false;
    bool resizing = false;
    bool isModified = false;

    Color normalColor = Colors.White;
    Color errorColor = Color.FromArgb("#FFD6D6");

    public MainPage()
    {
        InitializeComponent();
        CreateGrid();
        UpdateSizeBoxes();
        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, EventArgs e)
    {
#if WINDOWS
        var nativeWindow = Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (nativeWindow != null)
        {
            nativeWindow.AppWindow.Closing += AppWindow_Closing;
        }
#endif
    }

#if WINDOWS
    private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (closeConfirmed)
        {
            return;
        }

        args.Cancel = true;
        if (await AskToSaveAsync("Зберегти таблицю перед виходом?"))
        {
            closeConfirmed = true;
            Application.Current?.Quit();
        }
    }
#endif

    private void CreateGrid()
    {
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(40)));

        UpdateGridSize();
        RefreshCells();
    }

    private void UpdateGridSize()
    {
        while (builtColumns > table.ColumnCount)
        {
            RemoveLastColumn();
        }
        while (builtRows > table.RowCount)
        {
            RemoveLastRow();
        }
        while (builtColumns < table.ColumnCount)
        {
            AddColumnToGrid();
        }
        while (builtRows < table.RowCount)
        {
            AddRowToGrid();
        }
    }

    private void AddColumnToGrid()
    {
        int col = builtColumns;
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(CellWidth)));

        var label = MakeLabel(TableData.GetColumnName(col + 1));
        grid.Add(label, col + 1, 0);
        columnLabels.Add(label);

        for (int row = 0; row < builtRows; row++)
        {
            AddEntry(col, row);
        }
        builtColumns++;
    }

    private void AddRowToGrid()
    {
        int row = builtRows;
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(CellHeight)));

        var label = MakeLabel((row + 1).ToString());
        grid.Add(label, 0, row + 1);
        rowLabels.Add(label);

        for (int col = 0; col < builtColumns; col++)
        {
            AddEntry(col, row);
        }
        builtRows++;
    }

    private void RemoveLastColumn()
    {
        int col = builtColumns - 1;
        grid.Remove(columnLabels[col]);
        columnLabels.RemoveAt(col);

        for (int row = 0; row < builtRows; row++)
        {
            RemoveEntry(TableData.MakeAddress(col, row));
        }
        grid.ColumnDefinitions.RemoveAt(col + 1);
        builtColumns--;
    }

    private void RemoveLastRow()
    {
        int row = builtRows - 1;
        grid.Remove(rowLabels[row]);
        rowLabels.RemoveAt(row);

        for (int col = 0; col < builtColumns; col++)
        {
            RemoveEntry(TableData.MakeAddress(col, row));
        }
        grid.RowDefinitions.RemoveAt(row + 1);
        builtRows--;
    }

    private void AddEntry(int col, int row)
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
        entry.Completed += Entry_Completed;
        entry.HandlerChanged += Entry_HandlerChanged;

        grid.Add(entry, col + 1, row + 1);
        entries[address] = entry;
    }

    private void RemoveEntry(string address)
    {
        if (entries.TryGetValue(address, out var entry))
        {
            entry.Focused -= Entry_Focused;
            entry.Unfocused -= Entry_Unfocused;
            entry.Completed -= Entry_Completed;
            entry.HandlerChanged -= Entry_HandlerChanged;
            grid.Remove(entry);
            entries.Remove(address);
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

            string text = GetDisplayText(pair.Key);
            if (entry.Text != text)
            {
                entry.Text = text;
            }

            Color color = table.GetError(pair.Key) != null ? errorColor : normalColor;
            if (entry.BackgroundColor != color)
            {
                entry.BackgroundColor = color;
            }
        }
        updating = false;
    }

    private void ShowCellStatus(string address)
    {
        statusLabel.Text = table.GetError(address) ?? "";
    }

    private void TableChanged(string message)
    {
        if (!table.IsAddressInRange(selectedAddress))
        {
            selectedAddress = "A1";
            addressLabel.Text = selectedAddress;
        }
        UpdateGridSize();
        RefreshCells();
        UpdateSizeBoxes();
        statusLabel.Text = message;
    }

    private void UpdateSizeBoxes()
    {
        rowsEntry.Text = table.RowCount.ToString();
        columnsEntry.Text = table.ColumnCount.ToString();
    }

    private async Task ApplySizeAsync()
    {
        if (resizing)
        {
            return;
        }
        resizing = true;

        bool rowsOk = int.TryParse(rowsEntry.Text, out int rows);
        bool columnsOk = int.TryParse(columnsEntry.Text, out int columns);

        if (!rowsOk || !columnsOk || rows < 1 || rows > TableData.MaxRows || columns < 1 || columns > TableData.MaxColumns)
        {
            statusLabel.Text = "Введіть кількість рядків і стовпчиків від 1 до " + TableData.MaxRows;
            UpdateSizeBoxes();
            resizing = false;
            return;
        }

        if (rows != table.RowCount || columns != table.ColumnCount)
        {
            bool ok = true;
            if (table.HasDataOutside(rows, columns))
            {
                ok = await DisplayAlertAsync("Підтвердження", "У частині таблиці, що зникне, є дані. Продовжити?", "Так", "Ні");
            }

            if (ok)
            {
                table.Resize(rows, columns);
                isModified = true;
                TableChanged("");
            }
            else
            {
                UpdateSizeBoxes();
            }
        }
        resizing = false;
    }

    private async void Size_Completed(object sender, EventArgs e)
    {
        await ApplySizeAsync();
    }

    private async void Size_Unfocused(object sender, FocusEventArgs e)
    {
        await ApplySizeAsync();
    }

    private void FocusEntry(int col, int row)
    {
        var entry = entries[TableData.MakeAddress(col, row)];
        entry.Focus();
        Dispatcher.Dispatch(() => entry.CursorPosition = entry.Text?.Length ?? 0);
    }

    private void Entry_HandlerChanged(object sender, EventArgs e)
    {
#if WINDOWS
        var entry = (Entry)sender;
        if (entry.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
        {
            textBox.KeyDown += (s, args) => TextBox_KeyDown(entry, textBox, args);
        }
#endif
    }

#if WINDOWS
    private void TextBox_KeyDown(Entry entry, Microsoft.UI.Xaml.Controls.TextBox textBox, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs args)
    {
        if (args.Key != Windows.System.VirtualKey.Back || textBox.Text.Length > 0)
        {
            return;
        }
        if (!TableData.TryParseAddress(entry.ClassId, out int col, out int row))
        {
            return;
        }

        row--;
        if (row < 0)
        {
            row = table.RowCount - 1;
            col--;
        }
        if (col < 0)
        {
            return;
        }

        args.Handled = true;
        FocusEntry(col, row);
    }
#endif

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
        if (!CommitEntry(entry))
        {
            updating = true;
            entry.Text = GetDisplayText(entry.ClassId);
            updating = false;
        }
    }

    private bool CommitEntry(Entry entry)
    {
        string address = entry.ClassId;
        string text = entry.Text ?? "";

        if (text == table.GetExpression(address))
        {
            return false;
        }

        table.SetExpression(address, text);
        isModified = true;
        RefreshCells();
        ShowCellStatus(address);
        return true;
    }

    private void Entry_Completed(object sender, EventArgs e)
    {
        var entry = (Entry)sender;
        CommitEntry(entry);

        if (!TableData.TryParseAddress(entry.ClassId, out int col, out int row))
        {
            return;
        }

        row++;
        if (row >= table.RowCount)
        {
            row = 0;
            col++;
        }
        if (col >= table.ColumnCount)
        {
            return;
        }

        entries[TableData.MakeAddress(col, row)].Focus();
    }

    private void TextInput_Completed(object sender, EventArgs e)
    {
        string text = textInput.Text ?? "";
        if (text != table.GetExpression(selectedAddress))
        {
            isModified = true;
        }
        table.SetExpression(selectedAddress, text);
        RefreshCells();
        ShowCellStatus(selectedAddress);
    }

    private async Task<bool> SaveToFileAsync()
    {
        try
        {
#if WINDOWS
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            picker.SuggestedFileName = "table";
            picker.FileTypeChoices.Add("Таблиця Haxcel (JSON)", new List<string> { ".json" });

            var nativeWindow = Window.Handler.PlatformView as Microsoft.UI.Xaml.Window;
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow));

            var file = await picker.PickSaveFileAsync();
            if (file == null)
            {
                return false;
            }

            await File.WriteAllTextAsync(file.Path, table.ToJson());
            isModified = false;
            statusLabel.Text = "Збережено: " + file.Path;
            return true;
#else
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(table.ToJson()));
            var result = await FileSaver.Default.SaveAsync(desktop, "table.json", stream, CancellationToken.None);

            if (result.IsSuccessful)
            {
                isModified = false;
                statusLabel.Text = "Збережено: " + result.FilePath;
                return true;
            }
            if (result.Exception != null && result.Exception is not OperationCanceledException)
            {
                await DisplayAlertAsync("Помилка збереження", result.Exception.Message, "OK");
            }
            return false;
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Помилка збереження", ex.Message, "OK");
            return false;
        }
    }

    private async Task<bool> AskToSaveAsync(string question)
    {
        string answer = await DisplayActionSheetAsync(question, "Скасувати", null, "Так", "Ні");

        if (answer == "Так")
        {
            return await SaveToFileAsync();
        }
        return answer == "Ні";
    }

    private async Task<bool> CanReplaceTableAsync()
    {
        if (!isModified)
        {
            return true;
        }
        return await AskToSaveAsync("У поточній таблиці є незбережені зміни. Зберегти їх?");
    }

    private async void NewButton_Clicked(object sender, EventArgs e)
    {
        if (await CanReplaceTableAsync())
        {
            table = new TableData(CountRow, CountColumn);
            isModified = false;
            TableChanged("Створено нову таблицю");
        }
    }

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        await SaveToFileAsync();
    }

    private async void ReadButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!await CanReplaceTableAsync())
            {
                return;
            }

            var file = await FilePicker.Default.PickAsync();
            if (file == null)
            {
                return;
            }

            table = TableData.Load(file.FullPath);
            isModified = false;
            TableChanged("Прочитано: " + file.FullPath);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Помилка читання", ex.Message, "OK");
        }
    }

    private async void ExitButton_Clicked(object sender, EventArgs e)
    {
        if (await AskToSaveAsync("Зберегти таблицю перед виходом?"))
        {
            closeConfirmed = true;
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
            isModified = true;
            TableChanged("");
        }
        else
        {
            statusLabel.Text = "Максимальна кількість рядків: " + TableData.MaxRows;
        }
    }

    private void AddColumnButton_Clicked(object sender, EventArgs e)
    {
        if (table.AddColumn())
        {
            isModified = true;
            TableChanged("");
        }
        else
        {
            statusLabel.Text = "Максимальна кількість стовпчиків: " + TableData.MaxColumns;
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
            isModified = true;
            TableChanged("");
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
            isModified = true;
            TableChanged("");
        }
    }

    private async void HelpButton_Clicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Довідка",
            "Клікніть на клітинку, введіть вираз і натисніть Enter: курсор перейде в клітинку нижче. Backspace у порожній клітинці повертає на попередню.\n\n" +
            "Кількість рядків і стовпчиків можна ввести у полях угорі (Enter або перехід в інше поле).\n\n" +
            "У виразі: цілі числа, адреси клітинок (A1), дужки, операції + - * / mod div, унарні + -, max(x, y), min(x, y).\n\n" +
            "Помилки (клітинка червона, текст внизу): синтаксична з позицією, ділення на нуль, циклічне посилання.\n\n" +
            "Максимальний розмір таблиці: " + TableData.MaxRows + " на " + TableData.MaxColumns + ".\n" +
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
