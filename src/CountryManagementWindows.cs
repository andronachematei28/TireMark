using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TireMark;

internal sealed class CountryAddDialog : Window
{
    public CountryAddDialog(string countryName)
    {
        Title = "Add country";
        Width = 490;
        SizeToContent = SizeToContent.Height;
        MinWidth = 430;
        MaxWidth = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = Brush(Appearance.WindowColor);

        var body = new StackPanel { Margin = new Thickness(24) };
        body.Children.Add(MainWindow.Text("Add country", 21, true, Brush(Appearance.InkColor), new Thickness(0, 0, 0, 12)));
        body.Children.Add(MainWindow.Text($"Do you want to add {countryName} to your list?", 15, false, Brush(Appearance.InkColor), new Thickness(0, 0, 0, 20)));
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancel", Style = (Style)Application.Current.MainWindow.FindResource("GhostButton"), Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        var yes = new Button { Content = "Yes", Style = (Style)Application.Current.MainWindow.FindResource("PrimaryButton") };
        yes.Click += (_, _) => { DialogResult = true; Close(); };
        actions.Children.Add(cancel);
        actions.Children.Add(yes);
        body.Children.Add(actions);
        Content = body;
        Appearance.Apply(this);
    }

    private static Brush Brush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;
}

internal sealed class CountryPickerWindow : Window
{
    private readonly Action<string> _select;
    private readonly Action<string> _delete;
    private readonly Action<string> _add;
    private readonly StackPanel _rows = new();

    public CountryPickerWindow(Action<string> select, Action<string> delete, Action<string> add)
    {
        _select = select;
        _delete = delete;
        _add = add;
        Title = Localization.T("Countries");
        Width = 430;
        Height = 570;
        MinWidth = 360;
        MinHeight = 380;
        ResizeMode = ResizeMode.CanResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush(Appearance.WindowColor);

        var root = new DockPanel { Margin = new Thickness(22) };
        var header = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var heading = MainWindow.Text("Countries", 23, true, Brush(Appearance.InkColor), new Thickness(0));
        heading.VerticalAlignment = VerticalAlignment.Center;
        header.Children.Add(heading);
        var addCountry = new Button
        {
            Content = "+",
            ToolTip = Localization.T("Add country"),
            Style = (Style)Application.Current.MainWindow.FindResource("PrimaryButton"),
            Width = 50,
            Height = 50,
            MinWidth = 50,
            MinHeight = 50,
            Padding = new Thickness(0),
            FontSize = 26 * AppSettings.Current.TextScale,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        addCountry.Click += (_, _) => OpenAddCountry();
        Grid.SetColumn(addCountry, 1);
        header.Children.Add(addCountry);
        var headingContainer = header;
        DockPanel.SetDock(headingContainer, Dock.Top);
        root.Children.Add(headingContainer);
        root.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _rows });
        Content = root;
        RefreshRows();
        Appearance.Apply(this);
    }

    private void RefreshRows()
    {
        _rows.Children.Clear();
        foreach (var code in AppSettings.Current.CountryProfiles.OrderBy(AppSettings.CountryName, StringComparer.CurrentCultureIgnoreCase))
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 9) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var active = code == AppSettings.Current.Country;
            var select = new Button
            {
                Content = active ? $"{AppSettings.CountryName(code)}  ·  {Localization.T("Current country")}" : AppSettings.CountryName(code),
                Style = (Style)Application.Current.MainWindow.FindResource(active ? "PrimaryButton" : "GhostButton"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 8, 0)
            };
            select.Click += (_, _) => { _select(code); Close(); };
            row.Children.Add(select);

            var delete = new Button
            {
                Content = "×",
                ToolTip = Localization.T("Delete"),
                Style = (Style)Application.Current.MainWindow.FindResource("GhostButton"),
                Width = 42,
                Height = 42,
                MinWidth = 42,
                MinHeight = 42,
                Padding = new Thickness(0),
                FontSize = 19 * AppSettings.Current.TextScale,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(delete, 2);
            delete.Click += (_, _) => ConfirmDelete(code);
            row.Children.Add(delete);
            _rows.Children.Add(row);
        }
    }

    private void OpenAddCountry()
    {
        var picker = new CountrySelectionWindow(_add) { Owner = this };
        if (picker.ShowDialog() == true) RefreshRows();
    }

    private void ConfirmDelete(string code)
    {
        if (AppSettings.Current.CountryProfiles.Count <= 1)
        {
            new MessageDialogWindow("Keep one country", "Keep country message", information: true) { Owner = this }.ShowDialog();
            return;
        }
        var question = Localization.T("Delete country confirmation").Replace("{0}", AppSettings.CountryName(code), StringComparison.Ordinal);
        var dialog = new MessageDialogWindow("Delete country", question, translateMessage: false) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _delete(code);
            RefreshRows();
        }
    }

    private static Brush Brush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;
}

internal sealed class CountrySelectionWindow : Window
{
    private sealed record CountryChoice(string Code, string Name);

    private readonly Action<string> _select;
    private readonly TextBox _search = new();
    private readonly ComboBox _country = new() { MinWidth = 300, Padding = new Thickness(9), FontSize = 14 };
    private readonly TextBlock _searchHint;
    private readonly Button _done;
    private bool _refreshing;

    public CountrySelectionWindow(Action<string> select)
    {
        _select = select;
        Title = Localization.T("Add country");
        Width = 720;
        Height = 270;
        MinWidth = 620;
        MinHeight = 260;
        ResizeMode = ResizeMode.CanResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush(Appearance.WindowColor);

        var body = new DockPanel { Margin = new Thickness(24) };
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 20, 0, 0) };
        _done = new Button
        {
            Content = Localization.T("Done"),
            Style = (Style)Application.Current.MainWindow.FindResource("PrimaryButton"),
            MinWidth = 100,
            IsEnabled = false
        };
        _done.Click += (_, _) => CommitSelection();
        footer.Children.Add(_done);
        DockPanel.SetDock(footer, Dock.Bottom);
        body.Children.Add(footer);

        var content = new StackPanel();
        content.Children.Add(MainWindow.Text("Country or region", 13, true, Brush(Appearance.InkColor), new Thickness(0, 0, 0, 6)));
        _search.Style = (Style)Application.Current.MainWindow.FindResource("SearchBox");
        _search.ToolTip = Localization.T("Search countries");
        _searchHint = new TextBlock
        {
            Text = Localization.T("Search countries"),
            Margin = new Thickness(12, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brush(Appearance.MutedColor),
            FontSize = 13 * AppSettings.Current.TextScale,
            IsHitTestVisible = false
        };
        _search.TextChanged += (_, _) => RefreshCountries();
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.12, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(.88, GridUnitType.Star) });
        var searchHost = new Grid { Margin = new Thickness(10, 0, 0, 0), MinWidth = 150 };
        searchHost.Children.Add(_search);
        searchHost.Children.Add(_searchHint);
        Grid.SetColumn(searchHost, 1);
        row.Children.Add(_country);
        row.Children.Add(searchHost);
        content.Children.Add(row);
        body.Children.Add(content);
        _country.DisplayMemberPath = nameof(CountryChoice.Name);
        _country.SelectedValuePath = nameof(CountryChoice.Code);
        SettingsWindow.ConfigureDropdown(_country);
        _country.Background = Brush(Appearance.SurfaceColor);
        _country.Foreground = Brush(Appearance.InkColor);
        _country.BorderBrush = Brush(Appearance.LineColor);
        _country.SelectionChanged += (_, _) => UpdateSelection();
        Content = body;
        RefreshCountries();
        Appearance.Apply(this);
    }

    private void RefreshCountries()
    {
        var query = _search.Text.Trim();
        var existing = AppSettings.Current.CountryProfiles.ToHashSet(StringComparer.Ordinal);
        var matches = AppSettings.Countries
            .Where(country => !existing.Contains(country.Code) && country.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(country => country.Name, StringComparer.OrdinalIgnoreCase)
            .Select(country => new CountryChoice(country.Code, country.Name))
            .ToList();
        matches.Insert(0, new CountryChoice("", Localization.T("Select country")));
        _refreshing = true;
        _country.ItemsSource = matches;
        _country.SelectedValue = "";
        _refreshing = false;
        _searchHint.Visibility = string.IsNullOrEmpty(_search.Text) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateSelection()
    {
        if (_refreshing) return;
        _done.IsEnabled = _country.SelectedValue is string code && !string.IsNullOrWhiteSpace(code);
    }

    private void CommitSelection()
    {
        if (_country.SelectedValue is not string code || string.IsNullOrWhiteSpace(code)) return;
        _select(code);
        DialogResult = true;
        Close();
    }

    private static Brush Brush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;
}

internal sealed class MessageDialogWindow : Window
{
    public MessageDialogWindow(string titleKey, string message, bool translateMessage = true, bool information = false)
    {
        Title = Localization.T(titleKey);
        Width = 460;
        SizeToContent = SizeToContent.Height;
        MinWidth = 380;
        MaxWidth = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = Brush(Appearance.WindowColor);
        if (translateMessage) message = Localization.T(message);
        var body = new StackPanel { Margin = new Thickness(24) };
        body.Children.Add(MainWindow.Text(Title, 20, true, Brush(Appearance.InkColor), new Thickness(0, 0, 0, 12)));
        body.Children.Add(MainWindow.Text(message, 15, false, Brush(Appearance.InkColor), new Thickness(0, 0, 0, 20)));
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        if (information)
        {
            var ok = new Button { Content = Localization.T("OK"), Style = (Style)Application.Current.MainWindow.FindResource("PrimaryButton") };
            ok.Click += (_, _) => { DialogResult = false; Close(); };
            buttons.Children.Add(ok);
        }
        else
        {
            var cancel = new Button { Content = Localization.T("Cancel"), Style = (Style)Application.Current.MainWindow.FindResource("GhostButton"), Margin = new Thickness(0, 0, 8, 0) };
            cancel.Click += (_, _) => { DialogResult = false; Close(); };
            var confirm = new Button { Content = Localization.T("Delete"), Style = (Style)Application.Current.MainWindow.FindResource("PrimaryButton") };
            confirm.Click += (_, _) => { DialogResult = true; Close(); };
            buttons.Children.Add(cancel);
            buttons.Children.Add(confirm);
        }
        body.Children.Add(buttons);
        Content = body;
        Appearance.Apply(this);
    }

    private static Brush Brush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;
}
