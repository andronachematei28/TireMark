using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Threading;

namespace TireMark;

public partial class MainWindow : Window
{
    private readonly GarageDatabase _database;
    private readonly Dictionary<string, bool> _expanded = new();
    private string? _currentCarId;
    private WrapPanel? _carGrid;
    private UniformGrid? _categoryGrid;
    private Grid? _garageHeader;
    private Grid? _garageAddButton;
    private Grid? _heroGrid;
    private Border? _heroPhoto;
    private StackPanel? _heroDetails;
    private readonly DispatcherTimer _preferenceRenderTimer = new() { Interval = TimeSpan.FromMilliseconds(160) };
    private static IReadOnlyList<VehicleCategory> ActiveCategories => CountryCatalog.Find(AppSettings.Current.Country)?.Categories ?? [];
    private static Brush Ink = Brush("#142238");
    private static Brush Muted = Brush("#6B7B91");
    private static Brush Blue = Brush("#2864D7");
    private static Brush Line = Brush("#E1E7F0");

    internal MainWindow(GarageDatabase database)
    {
        InitializeComponent();
        _database = database;
        _preferenceRenderTimer.Tick += (_, _) =>
        {
            _preferenceRenderTimer.Stop();
            RenderCurrentPage();
        };
        SettingsButton.Click += (_, _) => OpenSettings();
        CountryButton.Click += (_, _) => OpenCountryPicker();
        ApplyPreferences();
        SizeChanged += (_, _) => UpdateResponsiveLayout();
        PageHost.SizeChanged += (_, _) => UpdateResponsiveLayout();
        Loaded += (_, _) => UpdateResponsiveLayout();
    }

    internal void Start() => RenderHome();

    internal void ApplyPreferences(bool renderPage = true)
    {
        Background = Brush(Appearance.WindowColor);
        HeaderBar.Background = Brush(Appearance.HeaderColor);
        HeaderBar.BorderBrush = Brush(Appearance.LineColor);
        BrandTitle.Foreground = Brush(Appearance.InkColor);
        TodayLabel.Foreground = Brush(Appearance.MutedColor);
        CategorySummary.Text = string.Join("  ·  ", ActiveCategories.Select(category => category.Label));
        CategorySummary.Foreground = Brush(Appearance.MutedColor);
        TodayLabel.Text = DateTime.Today.ToString("dddd, d MMMM yyyy", AppSettings.Culture);
        SettingsButton.Foreground = Brush(Appearance.MutedColor);
        SettingsButton.ToolTip = Localization.T("Settings");
        CountryButton.Content = $"{AppSettings.CountryName(AppSettings.Current.Country)}  ▾";
        CountryButton.Style = (Style)FindResource("PrimaryButton");
        Ink = Brush(Appearance.InkColor); Muted = Brush(Appearance.MutedColor); Line = Brush(Appearance.LineColor);
        Blue = Brush(AppSettings.Current.Accent);
        Resources["AccentBrush"] = Blue;
        Resources["AccentHoverBrush"] = Darken(Blue, .82);
        Resources["SurfaceBrush"] = Brush(Appearance.SurfaceColor); Resources["WindowBrush"] = Brush(Appearance.WindowColor);
        Resources["InkBrush"] = Ink; Resources["LineBrush"] = Line; Resources["HoverBrush"] = Brush(Appearance.AccentSurfaceColor);
        Resources["Ink"] = Ink; Resources["Muted"] = Muted; Resources["Line"] = Line; Resources["Blue"] = Blue;
        var factor = AppSettings.Current.TextScale;
        SetFontSize("PrimaryButton", 14 * factor); SetFontSize("GhostButton", 13 * factor); SetFontSize("InputBox", 14 * factor);
        TodayLabel.FontSize = 13 * factor; CategorySummary.FontSize = 12 * factor; SettingsButton.FontSize = 21 * factor; CountryButton.FontSize = 14 * factor;
        UpdateResponsiveLayout();
        if (renderPage && PageHost.Content is not null)
        {
            _preferenceRenderTimer.Stop();
            _preferenceRenderTimer.Start();
        }
    }

    private void RenderCurrentPage()
    {
        if (PageHost.Content is null) return;
        if (_currentCarId is null) RenderHome(); else RenderDetail(_currentCarId);
    }

    private void UpdateResponsiveLayout()
    {
        var width = ActualWidth > 0 ? ActualWidth : Width;
        var compact = width < 860;
        HeaderBar.Padding = compact ? new Thickness(14, 9, 14, 9) : new Thickness(24, 12, 24, 12);
        Grid.SetRow(TodayLabel, compact ? 1 : 0);
        Grid.SetColumn(TodayLabel, compact ? 0 : 1);
        Grid.SetColumnSpan(TodayLabel, compact ? 4 : 1);
        HeaderGrid.RowDefinitions[1].Height = compact ? GridLength.Auto : new GridLength(0);
        TodayLabel.Margin = compact ? new Thickness(0, 7, 0, 0) : new Thickness(0);
        CategorySummary.MaxWidth = Math.Max(160, width - (compact ? 105 : 150));
        CategorySummary.TextWrapping = TextWrapping.Wrap;
        if (PageHost.Content is FrameworkElement page) page.Width = PageWidth;
        if (_carGrid is not null) _carGrid.Width = PageWidth;
        if (_categoryGrid is not null) _categoryGrid.Columns = PageWidth < 760 ? 1 : PageWidth < 1180 ? 2 : 3;
        if (_heroGrid is not null && _heroPhoto is not null && _heroDetails is not null)
        {
            var narrow = PageWidth < 720;
            _heroGrid.ColumnDefinitions[0].Width = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(260);
            _heroGrid.RowDefinitions[1].Height = narrow ? GridLength.Auto : new GridLength(0);
            _heroPhoto.Width = narrow ? Math.Max(180, PageWidth - 48) : 260;
            _heroPhoto.Height = narrow ? Math.Max(110, _heroPhoto.Width * .625) : 162.5;
            _heroPhoto.Margin = narrow ? new Thickness(0, 0, 0, 16) : new Thickness(0, 0, 22, 0);
            Grid.SetRow(_heroDetails, narrow ? 1 : 0);
            Grid.SetColumn(_heroDetails, narrow ? 0 : 1);
        }
        if (_garageHeader is not null && _garageAddButton is not null)
        {
            var narrow = PageWidth < 700;
            _garageHeader.RowDefinitions[1].Height = narrow ? GridLength.Auto : new GridLength(0);
            Grid.SetRow(_garageAddButton, narrow ? 1 : 0);
            _garageAddButton.HorizontalAlignment = narrow ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            _garageHeader.Margin = new Thickness(0, 0, 0, narrow ? 18 : 24);
        }
    }

    private void SetFontSize(string key, double size)
    {
        var resourceKey = key switch { "PrimaryButton" => "PrimaryFontSize", "GhostButton" => "GhostFontSize", _ => "InputFontSize" };
        Resources[resourceKey] = size;
    }

    private void OpenSettings()
    {
        var settings = new SettingsWindow() { Owner = this };
        settings.ShowDialog();
    }

    private void OpenCountryPicker()
    {
        var picker = new CountryPickerWindow(ActivateCountry, DeleteCountry, AddCountry) { Owner = this };
        picker.ShowDialog();
    }

    private void ActivateCountry(string code)
    {
        if (!AppSettings.Current.CountryProfiles.Contains(code, StringComparer.Ordinal)) return;
        AppSettings.Current.Country = code;
        AppSettings.Save();
        AppSettings.ApplyCulture();
        ApplyPreferences(renderPage: false);
        RenderHome();
    }

    private void DeleteCountry(string code)
    {
        if (AppSettings.Current.CountryProfiles.Count <= 1) return;
        _database.DeleteCountry(code);
        AppSettings.Current.CountryProfiles.Remove(code);
        if (AppSettings.Current.Country == code) AppSettings.Current.Country = AppSettings.Current.CountryProfiles[0];
        AppSettings.Save();
        AppSettings.ApplyCulture();
        ApplyPreferences(renderPage: false);
        RenderHome();
    }

    private void AddCountry(string code)
    {
        if (CountryCatalog.Find(code) is null) return;
        if (!AppSettings.Current.CountryProfiles.Contains(code, StringComparer.Ordinal))
            AppSettings.Current.CountryProfiles.Add(code);
        AppSettings.Current.Country = code;
        AppSettings.Save();
        AppSettings.ApplyCulture();
        ApplyPreferences(renderPage: false);
        RenderHome();
    }

    internal void ApplyCountryChoice(string targetCountry)
    {
        if (CountryCatalog.Find(targetCountry) is null) return;
        if (!AppSettings.Current.CountryProfiles.Contains(targetCountry, StringComparer.Ordinal))
            AppSettings.Current.CountryProfiles.Add(targetCountry);
        AppSettings.Current.Country = targetCountry;
        AppSettings.Save();
        AppSettings.ApplyCulture();
        ApplyPreferences(renderPage: false);
        RenderHome();
    }

    private void RenderHome()
    {
        _currentCarId = null;
        _expanded.Clear();
        _categoryGrid = null;
        _garageHeader = null;
        _garageAddButton = null;
        _heroGrid = null;
        _heroPhoto = null;
        _heroDetails = null;
        var cars = _database.GetCars(AppSettings.Current.Country);
        var root = PageRoot();
        var heading = Heading($"{Localization.T("Garage")} {Localization.CarCount(cars.Count)}");
        var addButton = Positioned(Primary("＋   " + Localization.T("Add car"), AddCar_Click), horizontal: HorizontalAlignment.Right, vertical: VerticalAlignment.Center);
        heading.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        heading.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
        Grid.SetRow(addButton, 0);
        heading.Children.Add(addButton);
        _garageHeader = heading;
        _garageAddButton = addButton;
        root.Children.Add(heading);
        var wrap = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Left, Width = PageWidth };
        _carGrid = wrap;
        foreach (var car in cars) wrap.Children.Add(CarTile(car));
        root.Children.Add(wrap);
        LocalizeControls(root);
        PageHost.Content = root;
        UpdateResponsiveLayout();
    }

    internal static void LocalizeControls(DependencyObject node)
    {
        if (node is Window window) window.Title = Localization.T(window.Title);
        if (node is TextBlock block) block.Text = Localization.T(block.Text);
        if (node is Button button && button.Content is string buttonText) button.Content = Localization.T(buttonText);
        if (node is CheckBox check && check.Content is string checkText) check.Content = Localization.T(checkText);
        if (node is RadioButton radio && radio.Content is string radioText) radio.Content = Localization.T(radioText);
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) LocalizeControls(VisualTreeHelper.GetChild(node, i));
    }

    private void AddCar_Click(object sender, RoutedEventArgs e)
    {
        var editor = new CarEditorWindow(null) { Owner = this };
        if (editor.ShowDialog() != true || editor.Result is null) return;
        _database.SaveCar(editor.Result, AppSettings.Current.Country);
        RenderDetail(editor.Result.Id);
    }

    private Border CarTile(Car car)
    {
        var stack = new StackPanel();
        var imageFrame = new Border { Height = 258, CornerRadius = new CornerRadius(13), Background = Brush("#EAF0F8"), ClipToBounds = true, Margin = new Thickness(0, 0, 0, 16) };
        if (car.Photo is { Length: > 0 })
            imageFrame.Child = new Image { Source = LoadImage(car.Photo), Stretch = Stretch.UniformToFill };
        else
            imageFrame.Child = CenteredPlaceholder("FOTOGRAFIE MAȘINĂ", "#D8E5F8");
        stack.Children.Add(imageFrame);
        var nameRow = new Grid { Margin = new Thickness(1, 0, 0, 4) };
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        nameRow.Children.Add(Text(string.IsNullOrWhiteSpace(car.Name) ? "Mașină fără denumire" : car.Name, 18, true, Ink, new Thickness(0, 0, 8, 0), TextAlignment.Left, TextTrimming.CharacterEllipsis));
        var plate = new Border { Background = Brush("#EEF3FB"), CornerRadius = new CornerRadius(10), Padding = new Thickness(10, 6, 10, 6), Child = Text(string.IsNullOrWhiteSpace(car.Plate) ? "FĂRĂ NUMĂR" : car.Plate, 11, true, Blue) };
        Grid.SetColumn(plate, 1);
        nameRow.Children.Add(plate);
        stack.Children.Add(nameRow);
        stack.Children.Add(Text(string.IsNullOrWhiteSpace(car.Vin) ? "VIN necompletat" : $"VIN  {car.Vin}", 12, false, Muted, new Thickness(1, 2, 0, 12), TextAlignment.Left, TextTrimming.CharacterEllipsis));

        var chips = new WrapPanel { Margin = new Thickness(0, 0, 0, 11) };
        foreach (var type in ActiveCategories.Select(category => category.Id))
        {
            var latest = Latest(_database.GetRecords(car.Id, type), type);
            chips.Children.Add(StatusChip(TypeLabel(type), latest));
        }
        stack.Children.Add(chips);
        var bottom = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var open = new Button { Content = Localization.T("Deschide mașina  →"), Style = (Style)FindResource("PrimaryButton"), Padding = new Thickness(14, 9, 14, 9) };
        open.Click += (_, _) => RenderDetail(car.Id);
        Grid.SetColumn(open, 1);
        bottom.Children.Add(open);
        stack.Children.Add(bottom);

        var card = Card(stack, new Thickness(17));
        card.Width = Math.Min(448, PageWidth);
        card.Margin = new Thickness(0, 0, 18, 18);
        card.Cursor = Cursors.Hand;
        card.MouseLeftButtonUp += (_, e) =>
        {
            if (e.OriginalSource is not Button) RenderDetail(car.Id);
        };
        return card;
    }

    private void RenderDetail(string carId)
    {
        _currentCarId = carId;
        _carGrid = null;
        _garageHeader = null;
        _garageAddButton = null;
        var car = _database.GetCar(carId, AppSettings.Current.Country);
        if (car is null) { RenderHome(); return; }
        var root = PageRoot();
        var nav = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        nav.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        nav.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var back = Ghost("←   Înapoi la garaj", () => RenderHome());
        nav.Children.Add(back);
        var edit = Ghost("Editează mașina", () => EditCar(car));
        Grid.SetColumn(edit, 1);
        nav.Children.Add(edit);
        root.Children.Add(nav);

        var hero = new Grid { Margin = new Thickness(0, 0, 0, 24) };
        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hero.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        hero.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
        var photoBorder = new Border { Width = 260, Height = 162.5, CornerRadius = new CornerRadius(16), Background = Brush("#EAF0F8"), ClipToBounds = true, Margin = new Thickness(0, 0, 22, 0) };
        photoBorder.Child = car.Photo is { Length: > 0 } ? new Image { Source = LoadImage(car.Photo), Stretch = Stretch.UniformToFill } : CenteredPlaceholder("FOTOGRAFIE", "#D8E5F8");
        hero.Children.Add(photoBorder);
        var detail = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 10, 0) };
        detail.Children.Add(Text(car.Name, 25, true, Ink, new Thickness(0, 0, 0, 13)));
        detail.Children.Add(MetaLine("NUMĂR DE ÎNMATRICULARE", string.IsNullOrWhiteSpace(car.Plate) ? "Necompletat" : car.Plate));
        detail.Children.Add(MetaLine("VIN", string.IsNullOrWhiteSpace(car.Vin) ? "Necompletat" : car.Vin));
        Grid.SetColumn(detail, 1);
        hero.Children.Add(detail);
        _heroGrid = hero;
        _heroPhoto = photoBorder;
        _heroDetails = detail;
        root.Children.Add(Card(hero, new Thickness(18)));

        var categoryGrid = new UniformGrid { Columns = PageWidth < 760 ? 1 : PageWidth < 1180 ? 2 : 3, Margin = new Thickness(-8, 16, -8, 0), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Top };
        _categoryGrid = categoryGrid;
        foreach (var type in ActiveCategories.Select(category => category.Id)) categoryGrid.Children.Add(CategoryPanel(car, type));
        root.Children.Add(categoryGrid);
        LocalizeControls(root);
        PageHost.Content = root;
        UpdateResponsiveLayout();
    }

    private Border CategoryPanel(Car car, string type)
    {
        var records = _database.GetRecords(car.Id, type)
            .OrderByDescending(record => EffectiveDate(record, type))
            .ThenByDescending(record => record.CreatedAt)
            .ToList();
        var latest = records.FirstOrDefault();
        var panel = new StackPanel { Margin = new Thickness(8, 0, 8, 18), MinWidth = 0 };
        var heading = new Grid { Margin = new Thickness(2, 0, 2, 12) };
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var headingText = new StackPanel();
        headingText.Children.Add(RawText(TypeLabel(type), 17, true, Ink));
        heading.Children.Add(headingText);
        var add = new Button { Content = $"＋  {TypeLabel(type)}", Style = (Style)FindResource("GhostButton"), Padding = new Thickness(12, 9, 12, 9), Margin = new Thickness(8, 0, 0, 0) };
        add.Click += (_, _) => EditRecord(car, type, null);
        Grid.SetColumn(add, 1);
        heading.Children.Add(add);
        panel.Children.Add(heading);

        var expanded = _expanded.TryGetValue(type, out var isExpanded) && isExpanded;
        var status = GetStatus(latest);
        var statusButton = new Button { Padding = new Thickness(17), Background = status.Background, BorderBrush = status.Border, BorderThickness = new Thickness(1), Cursor = Cursors.Hand, HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 14) };
        statusButton.Template = (ControlTemplate)FindResource("CardButtonTemplate");
        var statusContent = new StackPanel();
        var statusHeader = new Grid();
        statusHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statusHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var statusText = ReadableText(status.Background);
        statusHeader.Children.Add(RawText(TypeLabel(type).ToUpperInvariant(), 10, true, statusText));
        statusHeader.Children.Add(new Border { Background = status.BadgeBackground, CornerRadius = new CornerRadius(10), Padding = new Thickness(10, 5, 10, 5), Child = Text(status.Badge, 11, true, ReadableText(status.BadgeBackground)) });
        Grid.SetColumn(statusHeader.Children[1], 1);
        statusContent.Children.Add(statusHeader);
        statusContent.Children.Add(Text(status.Message, 19, true, statusText, new Thickness(0, 14, 0, 5)));
        if (latest is not null)
        {
            var secondary = Text(status.Secondary, 12, false, statusText);
            secondary.Opacity = .88;
            statusContent.Children.Add(secondary);
        }
        if (expanded && latest is not null)
        {
            var detailText = RecordDetail(latest);
            statusContent.Children.Add(new Border { Background = status.BadgeBackground, CornerRadius = new CornerRadius(12), Padding = new Thickness(12), Margin = new Thickness(0, 13, 0, 0), Child = Text(detailText, 12, false, ReadableText(status.BadgeBackground)) });
        }
        statusButton.Content = statusContent;
        statusButton.Click += (_, _) =>
        {
            _expanded[type] = !_expanded.GetValueOrDefault(type);
            RenderDetail(car.Id);
        };
        panel.Children.Add(statusButton);

        if (records.Count > 0)
        {
            var list = new StackPanel();
            for (var i = 0; i < records.Count; i++)
                list.Children.Add(HistoryTile(car, type, records[i], i == 0));
            panel.Children.Add(list);
        }
        return Card(panel, new Thickness(15));
    }

    private Border HistoryTile(Car car, string type, Record record, bool latest)
    {
        var card = new Border { Background = latest ? Brush("#F5F8FF") : Brush(Appearance.SurfaceColor), BorderBrush = latest ? Brush(Appearance.LineColor) : Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Padding = new Thickness(13), Margin = new Thickness(0, 0, 0, 10), Cursor = Cursors.Hand };
        var stack = new StackPanel();
        var top = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        top.Children.Add(Text(latest ? "CEA MAI NOUĂ" : "ISTORIC", 10, true, latest ? Blue : Muted));
        if (record.PriceAmount.HasValue)
        {
            var badge = new Border { Background = Brush("#EDF1F7"), CornerRadius = new CornerRadius(9), Padding = new Thickness(9, 4, 9, 4), Child = Text(FormatAmount(record.PriceAmount.Value, record.CurrencyCode), 10, true, Muted) };
            Grid.SetColumn(badge, 1);
            top.Children.Add(badge);
        }
        stack.Children.Add(top);
        var category = CountryCatalog.Category(AppSettings.Current.Country, type);
        if (category?.HasPurchaseDate == true && record.PurchaseDate.HasValue) stack.Children.Add(DataLine("Cumpărată", Date(record.PurchaseDate)));
        if (category?.HasStartDate == true && record.StartDate.HasValue)
        {
            stack.Children.Add(DataLine("Intră în vigoare", Date(record.StartDate)));
        }
        if (category?.HasCompany == true && !string.IsNullOrWhiteSpace(record.Company)) stack.Children.Add(DataLine("Asigurator", record.Company));
        stack.Children.Add(DataLine("Expiră", Date(record.ExpiryDate)));
        stack.Children.Add(DataLine("Price", record.PriceAmount.HasValue ? FormatAmount(record.PriceAmount.Value, record.CurrencyCode) : Localization.T("Not entered")));
        card.Child = stack;
        card.MouseLeftButtonUp += (_, e) => { e.Handled = true; EditRecord(car, type, record); };
        return card;
    }

    private void EditCar(Car original)
    {
        var editor = new CarEditorWindow(original) { Owner = this };
        var result = editor.ShowDialog();
        if (editor.DeleteRequested)
        {
            if (MessageBox.Show(this, Localization.T("Ștergi mașina și toate intrările ei din istoric?"), Localization.T("Confirmă ștergerea"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _database.DeleteCar(original.Id);
                RenderHome();
            }
        }
        else if (result == true && editor.Result is not null)
        {
            _database.SaveCar(editor.Result, AppSettings.Current.Country);
            RenderDetail(original.Id);
        }
    }

    private void EditRecord(Car car, string type, Record? original)
    {
        var editor = new RecordEditorWindow(type, original) { Owner = this };
        var result = editor.ShowDialog();
        if (editor.DeleteRequested && original is not null)
        {
            if (MessageBox.Show(this, Localization.T("Ștergi această intrare din istoric?"), Localization.T("Confirmă ștergerea"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _database.DeleteRecord(original.Id);
                RenderDetail(car.Id);
            }
        }
        else if (result == true && editor.Result is not null)
        {
            _database.SaveRecord(editor.Result with { Id = original?.Id ?? Guid.NewGuid().ToString("N"), CarId = car.Id, Type = type, CreatedAt = original?.CreatedAt ?? DateTime.Now });
            RenderDetail(car.Id);
        }
    }

    private double PageWidth => Math.Max(280, (PageHost.ActualWidth > 0 ? PageHost.ActualWidth : (ActualWidth > 0 ? ActualWidth : Width)) - 56);
    private StackPanel PageRoot() => new() { Width = PageWidth, Margin = new Thickness(28, 24, 28, 42), HorizontalAlignment = HorizontalAlignment.Left };
    private static Border Card(UIElement content, Thickness? padding = null) => new() { Background = Brush(Appearance.SurfaceColor), BorderBrush = Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(20), Padding = padding ?? new Thickness(19), Child = content };
    private static Grid Heading(string title)
    {
        var grid = new Grid { VerticalAlignment = VerticalAlignment.Center };
        var stack = new StackPanel();
        stack.Children.Add(Text(title, 27, true, Ink));
        grid.Children.Add(stack);
        return grid;
    }

    internal static TextBlock Text(string text, double size = 13, bool bold = false, Brush? color = null, Thickness? margin = null, TextAlignment alignment = TextAlignment.Left, TextTrimming trimming = TextTrimming.None) =>
        new() { Text = Localization.T(text), FontSize = size * AppSettings.Current.TextScale, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = color ?? Ink, Margin = margin ?? new Thickness(0), TextAlignment = alignment, TextTrimming = trimming, TextWrapping = TextWrapping.Wrap, LineHeight = size * 1.42 * AppSettings.Current.TextScale };

    private static TextBlock RawText(string text, double size, bool bold, Brush? color = null, Thickness? margin = null) =>
        new() { Text = text, FontSize = size * AppSettings.Current.TextScale, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = color ?? Ink, Margin = margin ?? new Thickness(0), TextWrapping = TextWrapping.Wrap, LineHeight = size * 1.42 * AppSettings.Current.TextScale };

    private static Button Primary(string content, RoutedEventHandler click)
    {
        var button = new Button { Content = Localization.T(content), Style = (Style)Application.Current.MainWindow.FindResource("PrimaryButton"), FontSize = 14 * AppSettings.Current.TextScale };
        button.Click += click;
        return button;
    }

    private Button Ghost(string content, Action action)
    {
        var button = new Button { Content = Localization.T(content), Style = (Style)FindResource("GhostButton"), FontSize = 13 * AppSettings.Current.TextScale };
        button.Click += (_, _) => action();
        return button;
    }

    private static Grid Positioned(UIElement element, HorizontalAlignment horizontal, VerticalAlignment vertical)
    {
        var grid = new Grid { HorizontalAlignment = horizontal, VerticalAlignment = vertical };
        grid.Children.Add(element);
        return grid;
    }

    private static Border CenteredPlaceholder(string text, string background)
    {
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(Text("◈", 29, true, Brush("#7D9AC5"), new Thickness(0, 0, 0, 3), TextAlignment.Center));
        stack.Children.Add(Text(text, 10, true, Brush("#7D8FA8"), alignment: TextAlignment.Center));
        return new Border { Background = Brush(background), Child = stack };
    }

    private static Border StatusChip(string title, Record? record)
    {
        var status = GetStatus(record);
        var text = new TextBlock { Text = Localization.T($"●  {title}: {Localization.T(status.Badge)}"), FontSize = 10.5 * AppSettings.Current.TextScale, FontWeight = FontWeights.SemiBold, Foreground = ReadableText(status.BadgeBackground), TextWrapping = TextWrapping.NoWrap };
        return new Border { Background = status.BadgeBackground, BorderBrush = status.Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Padding = new Thickness(9, 6, 9, 6), Margin = new Thickness(0, 0, 6, 6), Child = text };
    }

    private static Brush ReadableText(Brush background)
    {
        var color = background is SolidColorBrush solid ? solid.Color : Colors.White;
        static double Linear(byte value)
        {
            var component = value / 255d;
            return component <= .04045 ? component / 12.92 : Math.Pow((component + .055) / 1.055, 2.4);
        }
        var luminance = .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
        var whiteContrast = 1.05 / (luminance + .05);
        var blackContrast = (luminance + .05) / .05;
        return whiteContrast >= blackContrast ? Brushes.White : Brushes.Black;
    }

    private static Border MetaLine(string label, string value)
    {
        var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(Text(label, 10, true, Muted));
        var data = Text(value, 13, true, Ink);
        Grid.SetColumn(data, 1); grid.Children.Add(data);
        return new Border { Child = grid };
    }

    private static Grid DataLine(string label, string value)
    {
        var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(Text(label, 11, false, Muted));
        var val = Text(value, 11, true, Ink, alignment: TextAlignment.Right);
        Grid.SetColumn(val, 1); grid.Children.Add(val);
        return grid;
    }

    private static Record? Latest(List<Record> records, string type) => records.OrderByDescending(record => EffectiveDate(record, type)).ThenByDescending(record => record.CreatedAt).FirstOrDefault();
    private static DateTime EffectiveDate(Record record, string type) => CountryCatalog.Category(AppSettings.Current.Country, type) is { HasPurchaseDate: true } ? record.PurchaseDate ?? record.ExpiryDate : CountryCatalog.Category(AppSettings.Current.Country, type) is { HasStartDate: true } ? record.StartDate ?? record.ExpiryDate : record.ExpiryDate;
    private static string TypeLabel(string type) => AppSettings.DocumentLabel(type);
    private static int GetDays(Record record) => (record.ExpiryDate.Date - DateTime.Today).Days;
    private static string Date(DateTime? date) => date?.ToString(AppSettings.CountryDatePattern, AppSettings.CountryCulture) ?? Localization.T("Not entered");
    private static string FormatAmount(decimal price, string currencyCode) => price.ToString("N2", AppSettings.Culture) + " " + currencyCode;
    internal static Brush Brush(string color)
    {
        var dark = Appearance.IsDark;
        var key = color.ToUpperInvariant();
        color = key switch
        {
            "#F2F5FA" => Appearance.WindowColor,
            "#FFFFFF" => Appearance.SurfaceColor,
            "#142238" or "#25364D" => Appearance.InkColor,
            "#6B7B91" or "#718198" or "#52647B" or "#7587A0" => Appearance.MutedColor,
            "#E1E7F0" or "#E5EAF1" or "#D8E1ED" or "#D9E1EC" => Appearance.LineColor,
            "#EAF0F8" or "#E4EBF5" or "#D8E5F8" or "#F5F8FF" or "#F7F9FC" or "#F4F7FC" or "#EDF1F7" or "#F6F8FB" or "#E9EDF3" or "#EEF3FB" => Appearance.AccentSurfaceColor,
            "#EAF1FF" or "#DBE8FF" => TintStatus(AppSettings.Current.Accent, dark ? "#191919" : "#EDF2FF", dark ? .18 : .08),
            "#D4E0F8" => Appearance.LineColor,
            "#FFF0EE" or "#FDE0DC" => TintStatus("#D74444", dark ? "#191919" : "#FFF0EE", dark ? .18 : .08),
            "#FFF3E2" or "#FFE5BD" or "#FFF5E7" or "#FFE7C2" => TintStatus("#C77818", dark ? "#191919" : "#FFF3E2", dark ? .18 : .08),
            "#EDF8F2" or "#D9F0E2" => TintStatus("#248A58", dark ? "#191919" : "#EDF8F2", dark ? .18 : .08),
            _ => color
        };
        return (Brush)new BrushConverter().ConvertFromString(color)!;
    }

    private static string TintStatus(string tintHex, string baseHex, double amount)
    {
        var tint = (Color)ColorConverter.ConvertFromString(tintHex)!;
        var baseColor = (Color)ColorConverter.ConvertFromString(baseHex)!;
        byte Mix(byte a, byte b) => (byte)Math.Clamp(Math.Round(a * (1 - amount) + b * amount), 0, 255);
        return $"#{Mix(tint.R, baseColor.R):X2}{Mix(tint.G, baseColor.G):X2}{Mix(tint.B, baseColor.B):X2}";
    }

    private static Brush Darken(Brush brush, double factor)
    {
        var color = ((SolidColorBrush)brush).Color;
        return new SolidColorBrush(Color.FromRgb((byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor)));
    }

    internal static BitmapImage LoadImage(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze();
        return image;
    }

    internal static (string Badge, string Message, string Secondary, Brush Background, Brush BadgeBackground, Brush Foreground, Brush Border) GetStatus(Record? record)
    {
        if (record is null) return ("No data", "History empty", "Add first record", Brush("#F6F8FB"), Brush("#E9EDF3"), Brush("#617187"), Brush("#E1E7F0"));
        if (CountryCatalog.Category(AppSettings.Current.Country, record.Type)?.HasStartDate == true && record.StartDate is DateTime start && start.Date > DateTime.Today)
        {
            var untilStart = (start.Date - DateTime.Today).Days;
            return ("Future", Localization.F("Starts in {0}", Localization.Days(untilStart)), Localization.F("Starts on {0} · Expires on {1}", Date(start), Date(record.ExpiryDate)), Brush("#EEF4FF"), Brush("#DBE8FF"), Brush("#2864D7"), Brush("#CDDCFA"));
        }
        var days = GetDays(record);
        if (days < 0) return ("Expired", "Expired", Localization.F("Expired: {0} ago · {1}", Localization.Days(Math.Abs(days)), Date(record.ExpiryDate)), Brush("#FFF0EE"), Brush("#FDE0DC"), Brush("#C9382C"), Brush("#F3C8C3"));
        if (days == 0) return ("Expires today", "Expires today", Localization.F("Expires on {0}", Date(record.ExpiryDate)), Brush("#FFF3E2"), Brush("#FFE5BD"), Brush("#B86A08"), Brush("#F2D4AA"));
        if (days <= 7) return (Localization.Days(days), Localization.F("Expires in {0}", Localization.Days(days)), Localization.F("Expires on {0}", Date(record.ExpiryDate)), Brush("#FFF5E7"), Brush("#FFE7C2"), Brush("#B86A08"), Brush("#F2D4AA"));
        return ("Valid", "Valid", Localization.F("Expires on {0}", Date(record.ExpiryDate)), Brush("#EDF8F2"), Brush("#D9F0E2"), Brush("#16814F"), Brush("#CBE7D7"));
    }

    internal static string RecordDetail(Record record)
    {
        var lines = new List<string>();
        var category = CountryCatalog.Category(AppSettings.Current.Country, record.Type);
        if (category?.HasPurchaseDate == true && record.PurchaseDate.HasValue) lines.Add(Localization.F("Purchased on {0}", Date(record.PurchaseDate)));
        if (category?.HasStartDate == true && record.StartDate.HasValue)
        {
            lines.Add(Localization.F("Effective from {0}", Date(record.StartDate)));
        }
        if (category?.HasCompany == true && !string.IsNullOrWhiteSpace(record.Company)) lines.Add($"{Localization.T("Insurer")}: {record.Company}");
        lines.Add(Localization.F("Expires on {0}", Date(record.ExpiryDate)));
        if (record.PriceAmount.HasValue) lines.Add(Localization.F("Price: {0}", FormatAmount(record.PriceAmount.Value, record.CurrencyCode)));
        return string.Join("\n", lines);
    }
}

internal sealed class CarEditorWindow : Window
{
    private readonly Car? _original;
    private readonly TextBox _name = new();
    private readonly TextBox _plate = new();
    private readonly TextBox _vin = new();
    private readonly Image _preview = new() { Stretch = Stretch.UniformToFill };
    private readonly Border _previewFrame;
    private byte[]? _photo;
    public Car? Result { get; private set; }
    public bool DeleteRequested { get; private set; }

    public CarEditorWindow(Car? original)
    {
        _original = original;
        _photo = original?.Photo;
        Title = Localization.T(original is null ? "Add car" : "Edit car");
        Width = 650; Height = 820; MinWidth = 480; MinHeight = 500; MaxHeight = 960; ResizeMode = ResizeMode.CanResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = Brush("#F2F5FA");
        var body = new StackPanel { Margin = new Thickness(30), MaxWidth = 570 };
        body.Children.Add(Text(Title, 26, true, Brush("#142238"), new Thickness(0, 0, 0, 5)));
        _previewFrame = new Border { MaxWidth = 480, Height = 300, HorizontalAlignment = HorizontalAlignment.Stretch, Background = Brush("#E4EBF5"), BorderBrush = Brush("#D8E1ED"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16), ClipToBounds = true, Child = _preview };
        if (_photo is { Length: > 0 }) _preview.Source = LoadImage(_photo);
        else _previewFrame.Child = EmptyPreview();
        body.Children.Add(_previewFrame);
        var photoButtons = new WrapPanel { Margin = new Thickness(0, 11, 0, 17) };
        var choose = GhostButton("Alege fotografia", ChoosePhoto); photoButtons.Children.Add(choose);
        if (_photo is { Length: > 0 }) photoButtons.Children.Add(GhostButton("Elimină fotografia", () => { _photo = null; _preview.Source = null; _previewFrame.Child = EmptyPreview(); }));
        body.Children.Add(photoButtons);
        body.Children.Add(Field("Denumire", _name, "Ex.: Toyota RAV4 2022", original?.Name));
        body.Children.Add(Field("Număr de înmatriculare", _plate, "Ex.: B 123 ABC", original?.Plate));
        body.Children.Add(Field("VIN", _vin, "Seria de șasiu", original?.Vin));
        var buttons = new WrapPanel { Margin = new Thickness(0, 12, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
        if (original is not null)
        {
            var delete = GhostButton("Șterge mașina", () => { DeleteRequested = true; DialogResult = false; Close(); });
            delete.Foreground = Brush("#B8342B"); buttons.Children.Add(delete);
        }
        buttons.Children.Add(GhostButton("Anulează", () => { DialogResult = false; Close(); }));
        buttons.Children.Add(Primary("Salvează", Save));
        body.Children.Add(buttons);
        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = body };
        MainWindow.LocalizeControls(this);
        Appearance.Apply(this);
        _name.Text = original?.Name ?? ""; _plate.Text = original?.Plate ?? ""; _vin.Text = original?.Vin ?? "";
    }

    private void ChoosePhoto()
    {
        var dialog = new OpenFileDialog { Filter = Localization.T("Images") + "|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff", Title = Localization.T("Alege fotografia mașinii") };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var source = new BitmapImage(); source.BeginInit(); source.CacheOption = BitmapCacheOption.OnLoad; source.UriSource = new Uri(dialog.FileName); source.EndInit(); source.Freeze();
            var crop = new PhotoCropWindow(source) { Owner = this };
            if (crop.ShowDialog() == true && crop.CroppedImage is { Length: > 0 } bytes)
            {
                _photo = bytes; _preview.Source = LoadImage(bytes); _previewFrame.Child = _preview;
            }
        }
        catch (Exception ex) { MessageBox.Show(this, Localization.T("Imaginea nu a putut fi deschisă.") + " " + ex.Message, Localization.T("Fotografie"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void Save(object sender, RoutedEventArgs e)
    {
        var name = _name.Text.Trim(); var plate = _plate.Text.Trim().ToUpperInvariant();
        if (name.Length == 0 && plate.Length == 0)
        {
            MessageBox.Show(this, Localization.T("Completează denumirea sau numărul de înmatriculare."), Localization.T("Date lipsă"), MessageBoxButton.OK, MessageBoxImage.Information); return;
        }
        Result = new Car(_original?.Id ?? Guid.NewGuid().ToString("N"), name, plate, _vin.Text.Trim().ToUpperInvariant(), _photo, _original?.CreatedAt ?? DateTime.Now);
        DialogResult = true; Close();
    }

    private static FrameworkElement Field(string label, TextBox box, string placeholder, string? value)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 13) };
        stack.Children.Add(Text(label, 12, true, Brush("#52647B"), new Thickness(0, 0, 0, 6)));
        box.Style = (Style)Application.Current.MainWindow.FindResource("InputBox"); box.Text = value ?? ""; box.ToolTip = Localization.T(placeholder);
        stack.Children.Add(box); return stack;
    }

    private static Button GhostButton(string label, Action action)
    {
        var button = new Button { Content = Localization.T(label), Style = (Style)Application.Current.MainWindow.FindResource("GhostButton"), Margin = new Thickness(0, 0, 8, 0) };
        button.Click += (_, _) => action(); return button;
    }

    private static Button Primary(string label, RoutedEventHandler action)
    {
        var button = new Button { Content = Localization.T(label), Style = (Style)Application.Current.MainWindow.FindResource("PrimaryButton") };
        button.Click += action; return button;
    }

    private static Border EmptyPreview() => new() { Background = Brush("#E4EBF5"), Child = new TextBlock { Text = Localization.T("Previzualizarea fotografiei"), FontSize = 13 * AppSettings.Current.TextScale, Foreground = Brush("#7587A0"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
    private static Brush Brush(string color) => MainWindow.Brush(color);
    private static BitmapImage LoadImage(byte[] bytes) => MainWindow.LoadImage(bytes);
    private static TextBlock Text(string text, double size, bool bold, Brush color, Thickness margin) => MainWindow.Text(text, size, bold, color, margin);
}

internal sealed class RecordEditorWindow : Window
{
    private readonly string _type;
    private readonly VehicleCategory _category;
    private readonly Record? _original;
    private readonly string _currencyCode;
    private readonly DatePicker _purchase = new();
    private readonly CheckBox _hasPurchase = new() { Content = Localization.T("Adaugă data cumpărării"), Margin = new Thickness(0, 7, 0, 0), FontSize = 13 * AppSettings.Current.TextScale };
    private readonly DatePicker _start = new();
    private readonly CheckBox _hasStart = new() { Content = Localization.T("Adaugă data intrării în vigoare (opțional)"), Margin = new Thickness(0, 7, 0, 0), FontSize = 13 * AppSettings.Current.TextScale };
    private readonly DatePicker _expiry = new();
    private readonly TextBox _price = new();
    private readonly TextBox _company = new();
    private readonly TextBlock _validation = new() { Foreground = Brush("#B86A08"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };
    public Record? Result { get; private set; }
    public bool DeleteRequested { get; private set; }

    public RecordEditorWindow(string type, Record? original)
    {
        _type = type; _category = CountryCatalog.Category(AppSettings.Current.Country, type) ?? new VehicleCategory(type, AppSettings.DocumentLabel(type));
        _original = original; _currencyCode = original?.CurrencyCode ?? AppSettings.CurrencyCode;
        Title = $"{Localization.T(original is null ? "Add" : "Edit")} {TypeLabel(type)}";
        Width = 640; Height = _category.HasPurchaseDate ? 630 : 650;
        MinHeight = 530; MaxHeight = 820; ResizeMode = ResizeMode.CanResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = Brush("#F2F5FA");
        _purchase.SelectedDate = original?.PurchaseDate;
        _hasPurchase.IsChecked = original?.PurchaseDate is not null;
        ConfigureCheckBox(_hasPurchase);
        _purchase.IsEnabled = _hasPurchase.IsChecked == true;
        _hasPurchase.Checked += (_, _) => _purchase.IsEnabled = true;
        _hasPurchase.Unchecked += (_, _) => { _purchase.IsEnabled = false; _purchase.SelectedDate = null; };
        _start.SelectedDate = original?.StartDate;
        _hasStart.IsChecked = original?.StartDate is not null;
        ConfigureCheckBox(_hasStart);
        _start.IsEnabled = _hasStart.IsChecked == true;
        _hasStart.Checked += (_, _) => _start.IsEnabled = true;
        _hasStart.Unchecked += (_, _) => { _start.IsEnabled = false; _start.SelectedDate = null; };
        _expiry.SelectedDate = original?.ExpiryDate;
        _price.Text = original?.PriceAmount?.ToString("0.##", AppSettings.Culture) ?? "";
        _company.Text = original?.Company ?? "";

        var body = new StackPanel { Margin = new Thickness(28), MaxWidth = 570 };
        body.Children.Add(Text(Title, 25, true, Brush("#142238"), new Thickness(0, 0, 0, 4)));
        if (_category.HasPurchaseDate)
        {
            body.Children.Add(Field("Data cumpărării (opțional)", _purchase));
            body.Children.Add(_hasPurchase);
            body.Children.Add(new Border { Height = 11, Background = Brushes.Transparent });
        }
        if (_category.HasStartDate)
        {
            body.Children.Add(Field("Data intrării în vigoare (opțional)", _start));
            body.Children.Add(_hasStart);
            body.Children.Add(new Border { Height = 11, Background = Brushes.Transparent });
        }
        body.Children.Add(Field("Data expirării", _expiry));
        if (_category.HasCompany) body.Children.Add(Field("Firma de asigurare (opțional)", _company, "Ex.: Allianz-Țiriac, Groupama"));
        body.Children.Add(Field("Preț (opțional)", _price, "Ex.: 350 sau 350,50"));
        body.Children.Add(_validation);

        var buttons = new WrapPanel { Margin = new Thickness(0, 18, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
        if (original is not null)
        {
            var delete = Ghost("Șterge intrarea", () => { DeleteRequested = true; DialogResult = false; Close(); });
            delete.Foreground = Brush("#B8342B"); buttons.Children.Add(delete);
        }
        buttons.Children.Add(Ghost("Anulează", () => { DialogResult = false; Close(); }));
        var save = new Button { Content = Localization.T("Salvează intrarea"), Style = (Style)Application.Current.MainWindow.FindResource("PrimaryButton") };
        save.Click += Save;
        buttons.Children.Add(save);
        body.Children.Add(buttons);
        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = body };
        MainWindow.LocalizeControls(this);
        Appearance.Apply(this);
    }

    private void Save(object sender, RoutedEventArgs e)
    {
        _validation.Text = "";
        DateTime? purchase = _category.HasPurchaseDate && _hasPurchase.IsChecked == true ? _purchase.SelectedDate?.Date : null;
        DateTime? start = _category.HasStartDate && _hasStart.IsChecked == true ? _start.SelectedDate?.Date : null;
        if (_category.HasStartDate && _hasStart.IsChecked == true && start is null) { _validation.Text = Localization.T("Alege data intrării în vigoare sau debifează opțiunea pentru a o lăsa necompletată."); return; }
        if (_expiry.SelectedDate is null) { _validation.Text = Localization.T("Alege data expirării."); return; }
        var expiry = _expiry.SelectedDate.Value.Date;
        if (purchase > expiry) { _validation.Text = Localization.T("Data cumpărării nu poate fi după data expirării."); return; }
        if (start > expiry) { _validation.Text = Localization.T("Data intrării în vigoare nu poate fi după data expirării."); return; }
        decimal? amount = null;
        if (!string.IsNullOrWhiteSpace(_price.Text))
        {
            var normalized = _price.Text.Trim().Replace(" ", "").Replace(',', '.');
            if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed) || parsed < 0 || parsed > 999999999)
            { _validation.Text = Localization.T("Scrie un preț valid, de exemplu 350 sau 350,50."); return; }
            amount = parsed;
        }
        Result = new Record(_original?.Id ?? Guid.NewGuid().ToString("N"), _original?.CarId ?? "", _type, purchase, start, expiry, amount, _currencyCode, _category.HasCompany ? _company.Text.Trim() : "", _original?.CreatedAt ?? DateTime.Now);
        DialogResult = true; Close();
    }

    private static FrameworkElement Field(string label, DatePicker picker)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        stack.Children.Add(Text(label, 12, true, Brush("#52647B"), new Thickness(0, 0, 0, 6)));
        picker.FontSize = 14 * AppSettings.Current.TextScale; picker.Padding = new Thickness(8, 5, 8, 5); picker.BorderBrush = Brush(Appearance.LineColor); picker.Background = Brush(Appearance.SurfaceColor); picker.Foreground = Brush(Appearance.InkColor);
        picker.MinHeight = 40;
        picker.HorizontalAlignment = HorizontalAlignment.Stretch;
        picker.VerticalContentAlignment = VerticalAlignment.Center;
        picker.SelectedDateFormat = DatePickerFormat.Short;
        picker.Language = XmlLanguage.GetLanguage(AppSettings.CountryCulture.Name);
        picker.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler((_, e) =>
        {
            if (FindVisualAncestor<DatePickerTextBox>(e.OriginalSource as DependencyObject) is null) return;
            picker.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (picker.IsEnabled) picker.IsDropDownOpen = true;
            }));
        }), true);
        picker.Resources[SystemColors.WindowBrushKey] = Brush(Appearance.SurfaceColor);
        picker.Resources[SystemColors.WindowTextBrushKey] = Brush(Appearance.InkColor);
        picker.Resources[typeof(DatePickerTextBox)] = new Style(typeof(DatePickerTextBox))
        {
            Setters =
            {
                new Setter(Control.BackgroundProperty, Brush(Appearance.SurfaceColor)),
                new Setter(Control.ForegroundProperty, Brush(Appearance.InkColor)),
                new Setter(Control.BorderBrushProperty, Brush(Appearance.LineColor)),
                new Setter(Control.FontSizeProperty, 14 * AppSettings.Current.TextScale),
                new Setter(Control.PaddingProperty, new Thickness(6, 3, 6, 3)),
                new Setter(TextBox.IsReadOnlyProperty, true),
                new Setter(TextBox.CaretBrushProperty, Brush(Appearance.InkColor))
            }
        };
        picker.Resources[SystemColors.ControlBrushKey] = Brush(Appearance.SurfaceColor);
        picker.Resources[SystemColors.ControlTextBrushKey] = Brush(Appearance.InkColor);
        stack.Children.Add(picker); return stack;
    }

    private static T? FindVisualAncestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node is not null)
        {
            if (node is T match) return match;
            DependencyObject? parent = null;
            try
            {
                if (node is Visual or System.Windows.Media.Media3D.Visual3D) parent = VisualTreeHelper.GetParent(node);
            }
            catch { }
            node = parent ?? LogicalTreeHelper.GetParent(node);
        }
        return null;
    }

    private static void ConfigureCheckBox(CheckBox checkBox)
    {
        checkBox.Foreground = Brush(Appearance.InkColor);
        checkBox.Resources[SystemColors.ControlTextBrushKey] = Brush(Appearance.InkColor);
        checkBox.Style = (Style)XamlReader.Parse("""
            <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="CheckBox">
              <Setter Property="Foreground" Value="{DynamicResource InkBrush}"/>
              <Setter Property="Template">
                <Setter.Value>
                  <ControlTemplate TargetType="CheckBox">
                    <StackPanel Orientation="Horizontal">
                      <Grid Width="16" Height="16" VerticalAlignment="Center">
                        <Border x:Name="Box" Background="{DynamicResource SurfaceBrush}" BorderBrush="{DynamicResource LineBrush}" BorderThickness="1" CornerRadius="2"/>
                        <Path x:Name="Mark" Data="M 2,8 L 6,12 L 14,3" Stroke="{DynamicResource CheckMarkBrush}" StrokeThickness="2" StrokeStartLineCap="Round" StrokeEndLineCap="Round" Visibility="Collapsed"/>
                      </Grid>
                      <ContentPresenter Margin="7,0,0,0" VerticalAlignment="Center" RecognizesAccessKey="True"/>
                    </StackPanel>
                    <ControlTemplate.Triggers>
                      <Trigger Property="IsChecked" Value="True">
                        <Setter TargetName="Mark" Property="Visibility" Value="Visible"/>
                        <Setter TargetName="Box" Property="BorderBrush" Value="{DynamicResource AccentBrush}"/>
                      </Trigger>
                      <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.55"/></Trigger>
                    </ControlTemplate.Triggers>
                  </ControlTemplate>
                </Setter.Value>
              </Setter>
            </Style>
            """);
    }

    private static FrameworkElement Field(string label, TextBox box, string placeholder)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        stack.Children.Add(Text(label, 12, true, Brush("#52647B"), new Thickness(0, 0, 0, 6)));
        box.Style = (Style)Application.Current.MainWindow.FindResource("InputBox"); box.ToolTip = Localization.T(placeholder);
        stack.Children.Add(box); return stack;
    }

    private static Button Ghost(string label, Action action)
    {
        var button = new Button { Content = label, Style = (Style)Application.Current.MainWindow.FindResource("GhostButton"), Margin = new Thickness(0, 0, 8, 0) };
        button.Click += (_, _) => action(); return button;
    }

    private static Brush Brush(string color) => MainWindow.Brush(color);
    private static TextBlock Text(string text, double size, bool bold, Brush color, Thickness margin) => MainWindow.Text(text, size, bold, color, margin);

    private static string TypeLabel(string type) => AppSettings.DocumentLabel(type);
}

internal sealed class PhotoCropWindow : Window
{
    private readonly CropSurface _surface;
    private readonly Slider _zoom;
    public byte[]? CroppedImage { get; private set; }

    public PhotoCropWindow(BitmapSource source)
    {
        Title = Localization.T("Decupează fotografia"); Width = 1040; Height = 780; MinWidth = 850; MinHeight = 650;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = Brush("#F2F5FA");
        var root = new DockPanel { Margin = new Thickness(24) };
        var footer = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 16, 0, 0) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = Localization.T("Anulează"), Style = (Style)Application.Current.MainWindow.FindResource("GhostButton"), Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        buttons.Children.Add(cancel);
        var use = new Button { Content = Localization.T("Folosește decupajul"), Style = (Style)Application.Current.MainWindow.FindResource("PrimaryButton") };
        use.Click += SaveCrop; buttons.Children.Add(use);
        DockPanel.SetDock(buttons, Dock.Right); footer.Children.Add(buttons);
        DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);

        var zoomBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12), VerticalAlignment = VerticalAlignment.Center };
        zoomBar.Children.Add(Text("Mărimea decupajului", 12, true, Brush("#52647B"), new Thickness(0, 0, 13, 0)));
        _zoom = new Slider { Minimum = 0.38, Maximum = 1, Value = 0.78, Width = 260, TickFrequency = 0.1, IsSnapToTickEnabled = false };
        zoomBar.Children.Add(_zoom);
        DockPanel.SetDock(zoomBar, Dock.Top); root.Children.Add(zoomBar);

        _surface = new CropSurface(source) { Margin = new Thickness(0), Background = Brush("#202A38") };
        _zoom.ValueChanged += (_, _) => _surface.ZoomFactor = _zoom.Value;
        DockPanel.SetDock(_surface, Dock.Top); root.Children.Add(_surface);
        Content = root;
        MainWindow.LocalizeControls(this);
        Appearance.Apply(this);
    }

    private void SaveCrop(object sender, RoutedEventArgs e)
    {
        try { CroppedImage = _surface.CreateJpeg(); DialogResult = true; Close(); }
        catch (Exception ex) { MessageBox.Show(this, Localization.T("Decuparea nu a putut fi salvată.") + " " + ex.Message, Localization.T("Fotografie"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private static Brush Brush(string color) => (Brush)new BrushConverter().ConvertFromString(color)!;
    private static TextBlock Text(string text, double size, bool bold, Brush color, Thickness margin) => new() { Text = text, FontSize = size, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = color, Margin = margin, VerticalAlignment = VerticalAlignment.Center };
}

internal sealed class CropSurface : Canvas
{
    private readonly BitmapSource _source;
    private readonly Image _image;
    private readonly Border _crop;
    private double _scale, _imageX, _imageY, _cropWidth, _cropHeight, _cropX, _cropY;
    private bool _dragging;
    private Point _dragStart;
    private double _dragCropX, _dragCropY;
    private double _zoomFactor = 0.78;

    public double ZoomFactor { get => _zoomFactor; set { _zoomFactor = Math.Clamp(value, 0.38, 1); UpdateCropSize(); } }

    public CropSurface(BitmapSource source)
    {
        _source = source;
        ClipToBounds = true;
        _image = new Image { Source = source, Stretch = Stretch.Fill, IsHitTestVisible = false };
        Children.Add(_image);
        _crop = new Border { BorderBrush = Brushes.White, BorderThickness = new Thickness(3), Background = new SolidColorBrush(Color.FromArgb(23, 45, 100, 215)), CornerRadius = new CornerRadius(3), Cursor = Cursors.SizeAll };
        _crop.MouseLeftButtonDown += CropDown;
        _crop.MouseMove += CropMove;
        _crop.MouseLeftButtonUp += CropUp;
        Children.Add(_crop);
        Loaded += (_, _) => LayoutImage();
        SizeChanged += (_, _) => LayoutImage();
    }

    private void LayoutImage()
    {
        if (ActualWidth < 10 || ActualHeight < 10) return;
        _scale = Math.Min(ActualWidth / _source.PixelWidth, ActualHeight / _source.PixelHeight);
        _image.Width = _source.PixelWidth * _scale; _image.Height = _source.PixelHeight * _scale;
        _imageX = (ActualWidth - _image.Width) / 2; _imageY = (ActualHeight - _image.Height) / 2;
        SetLeft(_image, _imageX); SetTop(_image, _imageY);
        UpdateCropSize();
    }

    private void UpdateCropSize()
    {
        if (_scale <= 0) return;
        var maxWidth = Math.Min(_image.Width, _image.Height * 1.6);
        _cropWidth = Math.Max(40, maxWidth * _zoomFactor);
        _cropHeight = _cropWidth / 1.6;
        _cropX = Math.Clamp(_cropX == 0 ? _imageX + (_image.Width - _cropWidth) / 2 : _cropX, _imageX, _imageX + _image.Width - _cropWidth);
        _cropY = Math.Clamp(_cropY == 0 ? _imageY + (_image.Height - _cropHeight) / 2 : _cropY, _imageY, _imageY + _image.Height - _cropHeight);
        _crop.Width = _cropWidth; _crop.Height = _cropHeight;
        SetLeft(_crop, _cropX); SetTop(_crop, _cropY);
    }

    private void CropDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true; _dragStart = e.GetPosition(this); _dragCropX = _cropX; _dragCropY = _cropY; _crop.CaptureMouse(); e.Handled = true;
    }
    private void CropMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var point = e.GetPosition(this);
        _cropX = Math.Clamp(_dragCropX + point.X - _dragStart.X, _imageX, _imageX + _image.Width - _cropWidth);
        _cropY = Math.Clamp(_dragCropY + point.Y - _dragStart.Y, _imageY, _imageY + _image.Height - _cropHeight);
        SetLeft(_crop, _cropX); SetTop(_crop, _cropY);
    }
    private void CropUp(object sender, MouseButtonEventArgs e) { _dragging = false; _crop.ReleaseMouseCapture(); }

    public byte[] CreateJpeg()
    {
        var x = Math.Clamp((int)Math.Round((_cropX - _imageX) / _scale), 0, _source.PixelWidth - 1);
        var y = Math.Clamp((int)Math.Round((_cropY - _imageY) / _scale), 0, _source.PixelHeight - 1);
        var width = Math.Clamp((int)Math.Round(_cropWidth / _scale), 1, _source.PixelWidth - x);
        var height = Math.Clamp((int)Math.Round(_cropHeight / _scale), 1, _source.PixelHeight - y);
        var cropped = new CroppedBitmap(_source, new Int32Rect(x, y, width, height));
        BitmapSource output = cropped;
        var shrink = Math.Min(1d, 1600d / Math.Max(width, height));
        if (shrink < 1) output = new TransformedBitmap(cropped, new ScaleTransform(shrink, shrink));
        var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
        encoder.Frames.Add(BitmapFrame.Create(output));
        using var stream = new MemoryStream(); encoder.Save(stream); return stream.ToArray();
    }
}



