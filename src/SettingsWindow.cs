using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Markup;

namespace TireMark;

internal sealed class SettingsWindow : Window
{
    private sealed record ColorChoice(string Target, string Value);
    private sealed record CatalogChoice(string Code, string Name);

    private readonly ComboBox _country = new() { MinWidth = 230, Padding = new Thickness(8) };
    private readonly ComboBox _language = new() { MinWidth = 230, Padding = new Thickness(8) };
    private readonly TextBox _countrySearch = new();
    private readonly TextBox _languageSearch = new();
    private readonly TextBlock _countrySearchHint;
    private readonly TextBlock _languageSearchHint;
    private readonly WrapPanel _accentPalette = new();
    private readonly WrapPanel _backgroundPalette = new();
    private readonly TextBlock _scaleValue = new() { VerticalAlignment = VerticalAlignment.Center, MinWidth = 52, TextAlignment = TextAlignment.Right };
    private readonly List<Button> _accentSwatches = [];
    private readonly List<Button> _backgroundSwatches = [];
    private bool _refreshing;

    public SettingsWindow()
    {
        Title = Localization.T("Settings");
        Width = 760;
        Height = 780;
        MinWidth = 620;
        MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;
        Background = Brush(Appearance.WindowColor);

        var root = new StackPanel { Margin = new Thickness(24), MaxWidth = 720 };
        root.Children.Add(Label("Settings", 24, true, new Thickness(0, 0, 0, 18)));
        root.Children.Add(Label("Region", 13, true, new Thickness(0, 0, 0, 5)));
        _countrySearch.Style = (Style)Application.Current.MainWindow.FindResource("SearchBox");
        _countrySearch.ToolTip = Localization.T("Search countries");
        _countrySearchHint = SearchHint("Search countries");
        _country.DisplayMemberPath = nameof(CountryInfo.Name);
        _country.SelectedValuePath = nameof(CountryInfo.Code);
        ConfigureDropdown(_country);
        root.Children.Add(SearchableRow(_country, _countrySearch, _countrySearchHint));

        root.Children.Add(Label("Language", 13, true, new Thickness(0, 16, 0, 5)));
        _languageSearch.Style = (Style)Application.Current.MainWindow.FindResource("SearchBox");
        _languageSearch.ToolTip = Localization.T("Search languages");
        _languageSearchHint = SearchHint("Search languages");
        _language.DisplayMemberPath = nameof(LanguageInfo.Name);
        _language.SelectedValuePath = nameof(LanguageInfo.Code);
        ConfigureDropdown(_language);
        root.Children.Add(SearchableRow(_language, _languageSearch, _languageSearchHint));

        root.Children.Add(Label("Appearance", 13, true, new Thickness(0, 18, 0, 5)));
        var themes = new StackPanel { Orientation = Orientation.Horizontal };
        var light = new RadioButton { Content = ThemeLabel("Light"), GroupName = "theme", Margin = new Thickness(0, 0, 22, 0) };
        var dark = new RadioButton { Content = ThemeLabel("Dark"), GroupName = "theme" };
        light.IsChecked = AppSettings.Current.Theme != "dark";
        dark.IsChecked = AppSettings.Current.Theme == "dark";
        light.Checked += (_, _) => SetTheme("light");
        dark.Checked += (_, _) => SetTheme("dark");
        themes.Children.Add(light);
        themes.Children.Add(dark);
        root.Children.Add(themes);

        root.Children.Add(Label("Accent color", 13, true, new Thickness(0, 16, 0, 5)));
        BuildPalette(_accentPalette, _accentSwatches, "accent");
        root.Children.Add(_accentPalette);
        root.Children.Add(CustomColorButton("Choose custom color", "accent", new Thickness(0, 2, 0, 0)));

        root.Children.Add(Label("Background color", 13, true, new Thickness(0, 14, 0, 5)));
        BuildPalette(_backgroundPalette, _backgroundSwatches, "background");
        root.Children.Add(_backgroundPalette);
        root.Children.Add(CustomColorButton("Choose custom color", "background", new Thickness(0, 2, 0, 0)));

        root.Children.Add(Label("Text size", 13, true, new Thickness(0, 16, 0, 4)));
        var scaleRow = new Grid();
        scaleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        scaleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var scale = new Slider
        {
            Minimum = .85,
            Maximum = 1.4,
            Value = Math.Clamp(AppSettings.Current.TextScale, .85, 1.4),
            TickFrequency = .05,
            IsSnapToTickEnabled = false,
            IsMoveToPointEnabled = true,
            SmallChange = .05,
            LargeChange = .1,
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        scale.ValueChanged += (_, _) =>
        {
            AppSettings.Current.TextScale = scale.Value;
            AppSettings.Save();
            _scaleValue.Text = $"{AppSettings.Current.TextScale:P0}";
            RefreshLanguage();
            Appearance.Apply(this);
        };
        Grid.SetColumn(scale, 0);
        scaleRow.Children.Add(scale);
        _scaleValue.Text = $"{AppSettings.Current.TextScale:P0}";
        Grid.SetColumn(_scaleValue, 1);
        scaleRow.Children.Add(_scaleValue);
        root.Children.Add(scaleRow);

        _countrySearch.TextChanged += (_, _) => RefreshCountryOptions();
        _languageSearch.TextChanged += (_, _) => RefreshLanguageOptions();
        _country.SelectionChanged += (_, _) => SelectCountry();
        _language.SelectionChanged += (_, _) => SelectLanguage();
        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };

        RefreshSwatches();
        RefreshCountryOptions();
        RefreshLanguageOptions();
        RefreshLanguage();
        Appearance.Apply(this);
    }

    private void BuildPalette(WrapPanel panel, List<Button> swatches, string target)
    {
        foreach (var color in ColorPalette.Generate())
        {
            var button = new Button
            {
                Width = 25,
                Height = 25,
                Margin = new Thickness(2),
                Padding = new Thickness(0),
                Background = Brush(color),
                BorderThickness = new Thickness(1),
                ToolTip = color,
                Tag = new ColorChoice(target, color),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            button.Click += (_, _) => SetColor(target, color);
            panel.Children.Add(button);
            swatches.Add(button);
        }
    }

    private static Grid SearchableRow(ComboBox combo, TextBox search, TextBlock hint)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.12, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(.88, GridUnitType.Star) });
        var searchHost = new Grid { Margin = new Thickness(10, 0, 0, 0), MinWidth = 150 };
        searchHost.Children.Add(search);
        searchHost.Children.Add(hint);
        Grid.SetColumn(searchHost, 1);
        row.Children.Add(combo);
        row.Children.Add(searchHost);
        return row;
    }

    private static TextBlock SearchHint(string key) => new()
    {
        Text = Localization.T(key),
        Tag = key,
        Margin = new Thickness(12, 0, 8, 0),
        VerticalAlignment = VerticalAlignment.Center,
        Foreground = Brush(Appearance.MutedColor),
        FontSize = 13 * AppSettings.Current.TextScale,
        IsHitTestVisible = false
    };

    private static void UpdateSearchHint(TextBox search, TextBlock hint) =>
        hint.Visibility = string.IsNullOrEmpty(search.Text) ? Visibility.Visible : Visibility.Collapsed;

    private Button CustomColorButton(string label, string target, Thickness margin)
    {
        var button = new Button
        {
            Content = Localization.T(label),
            Tag = label,
            Margin = margin,
            Padding = new Thickness(10, 5, 10, 5),
            HorizontalAlignment = HorizontalAlignment.Left,
            Style = Application.Current.MainWindow?.TryFindResource("GhostButton") as Style
        };
        button.Click += (_, _) => ChooseCustomColor(target);
        return button;
    }

    private void ChooseCustomColor(string target)
    {
        var current = target == "accent" ? AppSettings.Current.Accent : AppSettings.Current.BackgroundTint;
        var picker = new ColorPickerWindow(current, value => SetColor(target, value)) { Owner = this };
        picker.ShowDialog();
    }

    private void SetColor(string target, string value)
    {
        if (target == "accent") AppSettings.Current.Accent = value;
        else AppSettings.Current.BackgroundTint = value;
        AppSettings.Save();
        RefreshLanguage();
        Appearance.Apply(this);
    }

    private void RefreshSwatches()
    {
        var accent = AppSettings.Current.Accent;
        var background = AppSettings.Current.BackgroundTint;
        var selectedBorder = Brush(AppSettings.Current.Theme == "dark" ? "#F5F5F5" : "#101010");
        foreach (var swatch in _accentSwatches)
        {
            var selected = swatch.Tag is ColorChoice choice && string.Equals(choice.Value, accent, StringComparison.OrdinalIgnoreCase);
            swatch.BorderBrush = selected ? selectedBorder : Brush(Appearance.LineColor);
            swatch.BorderThickness = new Thickness(selected ? 2 : 1);
        }
        foreach (var swatch in _backgroundSwatches)
        {
            var selected = swatch.Tag is ColorChoice choice && string.Equals(choice.Value, background, StringComparison.OrdinalIgnoreCase);
            swatch.BorderBrush = selected ? selectedBorder : Brush(Appearance.LineColor);
            swatch.BorderThickness = new Thickness(selected ? 2 : 1);
        }
    }

    private void RefreshCountryOptions()
    {
        var query = _countrySearch.Text.Trim();
        var matches = AppSettings.Countries.Where(x => x.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)).OrderBy(x => x.Name).ToList();
        var items = new List<CatalogChoice> { new("", Localization.T("Select country")) };
        items.AddRange(matches.Select(x => new CatalogChoice(x.Code, x.Name)));
        _refreshing = true;
        _country.ItemsSource = items;
        _country.SelectedValue = matches.Any(x => x.Code == AppSettings.Current.Country) ? AppSettings.Current.Country : "";
        _refreshing = false;
        UpdateSearchHint(_countrySearch, _countrySearchHint);
    }

    private void RefreshLanguageOptions()
    {
        var query = _languageSearch.Text.Trim();
        var matches = AppSettings.Languages.Where(x => x.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)).OrderBy(x => x.Name).ToList();
        var items = new List<CatalogChoice> { new("", Localization.T("Select language")) };
        items.AddRange(matches.Select(x => new CatalogChoice(x.Code, x.Name)));
        _refreshing = true;
        _language.ItemsSource = items;
        _language.SelectedValue = matches.Any(x => x.Code == AppSettings.Current.Language) ? AppSettings.Current.Language : "";
        _refreshing = false;
        UpdateSearchHint(_languageSearch, _languageSearchHint);
    }

    private void SelectCountry()
    {
        if (_refreshing || _country.SelectedValue is not string code || string.IsNullOrEmpty(code) || code == AppSettings.Current.Country) return;
        if (AppSettings.Current.CountryProfiles.Contains(code, StringComparer.Ordinal))
        {
            if (Application.Current.MainWindow is MainWindow main)
                main.ApplyCountryChoice(code);
        }
        else
        {
            var prompt = new CountryAddDialog(AppSettings.CountryName(code)) { Owner = this };
            if (prompt.ShowDialog() == true && Application.Current.MainWindow is MainWindow ownerWindow)
                ownerWindow.ApplyCountryChoice(code);
        }
        RefreshLanguage();
        Appearance.Apply(this);
    }

    private void SelectLanguage()
    {
        if (_refreshing || _language.SelectedValue is not string code || string.IsNullOrEmpty(code) || code == AppSettings.Current.Language) return;
        AppSettings.Current.Language = code;
        AppSettings.Save();
        AppSettings.ApplyCulture();
        RefreshLanguage();
        Appearance.Apply(this);
    }

    private void RefreshLanguage()
    {
        if (Content is not DependencyObject root) return;
        RefreshCountryOptions();
        RefreshLanguageOptions();
        _refreshing = true;
        ConfigureDropdown(_country);
        ConfigureDropdown(_language);
        Title = Localization.T("Settings");
        foreach (var item in Enumerate(root))
        {
            if (item is TextBlock label && label.Tag is ValueTuple<string, double, bool> meta)
            {
                label.Text = Localization.T(meta.Item1);
                label.FontSize = meta.Item2 * AppSettings.Current.TextScale;
                label.FontWeight = meta.Item3 ? FontWeights.SemiBold : FontWeights.Normal;
                label.Foreground = Brush(Appearance.InkColor);
            }
            if (item is TextBlock themeLabel && themeLabel.Tag is string themeKey)
            {
                themeLabel.Text = Localization.T(themeKey);
                themeLabel.Foreground = Brush(themeKey is "Search countries" or "Search languages" ? Appearance.MutedColor : Appearance.InkColor);
                themeLabel.FontSize = (themeKey is "Search countries" or "Search languages" ? 13 : 14) * AppSettings.Current.TextScale;
            }
            if (item is Button button && button.Tag is string textKey) button.Content = Localization.T(textKey);
            if (item is RadioButton radio) radio.Foreground = Brush(Appearance.InkColor);
        }
        _country.FontSize = _language.FontSize = 15 * AppSettings.Current.TextScale;
        _country.Background = _language.Background = Brush(Appearance.SurfaceColor);
        _country.Foreground = _language.Foreground = Brush(Appearance.InkColor);
        _countrySearch.FontSize = _languageSearch.FontSize = 14 * AppSettings.Current.TextScale;
        _countrySearch.Background = _languageSearch.Background = Brush(Appearance.SurfaceColor);
        _countrySearch.Foreground = _languageSearch.Foreground = Brush(Appearance.InkColor);
        _countrySearch.ToolTip = Localization.T("Search countries");
        _languageSearch.ToolTip = Localization.T("Search languages");
        UpdateSearchHint(_countrySearch, _countrySearchHint);
        UpdateSearchHint(_languageSearch, _languageSearchHint);
        _scaleValue.FontSize = 13 * AppSettings.Current.TextScale;
        _scaleValue.Foreground = Brush(Appearance.InkColor);
        _refreshing = false;
        RefreshSwatches();
    }

    internal static void ConfigureDropdown(ComboBox combo)
    {
        combo.ItemTemplate = null;
        var itemStyle = new Style(typeof(ComboBoxItem));
        itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brush(Appearance.SurfaceColor)));
        itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brush(Appearance.InkColor)));
        itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 5, 8, 5)));
        itemStyle.Triggers.Add(new Trigger { Property = ComboBoxItem.IsHighlightedProperty, Value = true, Setters = { new Setter(Control.BackgroundProperty, Brush(Appearance.AccentSurfaceColor)) } });
        combo.ItemContainerStyle = itemStyle;
        combo.Template = (ControlTemplate)XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="{x:Type ComboBox}">
              <Grid>
                <ToggleButton x:Name="DropButton" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" IsChecked="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}" ClickMode="Press">
                  <ToggleButton.Template>
                    <ControlTemplate TargetType="{x:Type ToggleButton}">
                      <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="5" Padding="{TemplateBinding Padding}">
                        <Grid><TextBlock Text="▾" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,7,0"/><ContentPresenter HorizontalAlignment="Stretch" VerticalAlignment="Center"/></Grid>
                      </Border>
                    </ControlTemplate>
                  </ToggleButton.Template>
                </ToggleButton>
                <ContentPresenter IsHitTestVisible="False" Content="{TemplateBinding SelectionBoxItem}" ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}" ContentTemplateSelector="{TemplateBinding ItemTemplateSelector}" Margin="10,0,30,0" VerticalAlignment="Center" HorizontalAlignment="Left"/>
                <Popup x:Name="PART_Popup" Placement="Bottom" PlacementTarget="{Binding RelativeSource={RelativeSource TemplatedParent}}" IsOpen="{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}}" AllowsTransparency="True" Focusable="False" PopupAnimation="Fade" StaysOpen="False">
                  <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1" MaxHeight="{TemplateBinding MaxDropDownHeight}">
                    <ScrollViewer Margin="2" SnapsToDevicePixels="True"><ItemsPresenter KeyboardNavigation.DirectionalNavigation="Contained"/></ScrollViewer>
                  </Border>
                </Popup>
              </Grid>
            </ControlTemplate>
            """);
        combo.Resources[SystemColors.WindowBrushKey] = Brush(Appearance.SurfaceColor);
        combo.Resources[SystemColors.WindowTextBrushKey] = Brush(Appearance.InkColor);
        combo.Resources[SystemColors.ControlBrushKey] = Brush(Appearance.SurfaceColor);
        combo.Resources[SystemColors.ControlTextBrushKey] = Brush(Appearance.InkColor);
    }

    private static IEnumerable<DependencyObject> Enumerate(DependencyObject root)
    {
        yield return root;
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Enumerate(System.Windows.Media.VisualTreeHelper.GetChild(root, i))) yield return child;
    }

    private void SetTheme(string value)
    {
        AppSettings.Current.Theme = value;
        AppSettings.Save();
        RefreshLanguage();
        Appearance.Apply(this);
    }

    private TextBlock ThemeLabel(string key) => new()
    {
        Text = Localization.T(key),
        Tag = key,
        Foreground = Brush(Appearance.InkColor),
        FontSize = 14 * AppSettings.Current.TextScale
    };

    private TextBlock Label(string text, double size, bool bold, Thickness margin) => new()
    {
        Text = Localization.T(text),
        Tag = (text, size, bold),
        FontSize = size * AppSettings.Current.TextScale,
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
        Foreground = Brush(Appearance.InkColor),
        Margin = margin,
        TextWrapping = TextWrapping.Wrap
    };

    private static Brush Brush(string value) => (Brush)new BrushConverter().ConvertFromString(value)!;
}

internal static class ColorPalette
{
    public static IReadOnlyList<string> Generate()
    {
        var colors = new List<string>();
        for (var hue = 0; hue < 360; hue += 15)
        {
            colors.Add(ToHex(Hsl(hue, .82, .52)));
            colors.Add(ToHex(Hsl(hue, .72, .70)));
        }
        for (var lightness = .12; lightness <= .92; lightness += .1)
        {
            var value = (byte)Math.Round(lightness * 255);
            colors.Add($"#{value:X2}{value:X2}{value:X2}");
        }
        return colors.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static System.Windows.Media.Color Hsl(double hue, double saturation, double lightness)
    {
        var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var x = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
        var m = lightness - chroma / 2;
        var (r, g, b) = hue switch
        {
            < 60 => (chroma, x, 0d), < 120 => (x, chroma, 0d), < 180 => (0d, chroma, x),
            < 240 => (0d, x, chroma), < 300 => (x, 0d, chroma), _ => (chroma, 0d, x)
        };
        return System.Windows.Media.Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
    }

    public static string ToHex(System.Windows.Media.Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}

internal sealed class ColorPickerWindow : Window
{
    private readonly Action<string> _onColorChanged;
    private readonly Slider _hue;
    private readonly Slider _saturation;
    private readonly Slider _brightness;
    private readonly TextBox _hex = new();
    private readonly Border _preview = new() { Height = 54, CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), Margin = new Thickness(0, 12, 0, 16) };
    private bool _updating;

    public ColorPickerWindow(string initialColor, Action<string> onColorChanged)
    {
        _onColorChanged = onColorChanged;
        Title = Localization.T("Choose custom color");
        Width = 430;
        Height = 430;
        MinWidth = 400;
        MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;
        Background = Brush(Appearance.WindowColor);

        var color = (Color)ColorConverter.ConvertFromString(initialColor)!;
        var (hue, saturation, brightness) = ToHsl(color);
        var root = new StackPanel { Margin = new Thickness(24) };
        root.Children.Add(Label("Hue", hue.ToString("0"), 360, hue));
        _hue = (Slider)((Border)root.Children[^1]).Tag;
        root.Children.Add(Label("Saturation", $"{saturation:P0}", 1, saturation));
        _saturation = (Slider)((Border)root.Children[^1]).Tag;
        root.Children.Add(Label("Brightness", $"{brightness:P0}", 1, brightness));
        _brightness = (Slider)((Border)root.Children[^1]).Tag;
        foreach (var slider in new[] { _hue, _saturation, _brightness }) slider.ValueChanged += (_, _) => UpdateColorFromSliders();

        root.Children.Add(new TextBlock { Text = "#RRGGBB", FontSize = 12 * AppSettings.Current.TextScale, Foreground = Brush(Appearance.InkColor), Margin = new Thickness(0, 7, 0, 5) });
        _hex.Style = Application.Current.MainWindow?.TryFindResource("InputBox") as Style;
        _hex.Text = initialColor;
        _hex.TextChanged += (_, _) => UpdateColorFromHex();
        root.Children.Add(_hex);
        root.Children.Add(_preview);
        var done = new Button { Content = Localization.T("Done"), Style = Application.Current.MainWindow?.TryFindResource("PrimaryButton") as Style, HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 100 };
        done.Click += (_, _) => { DialogResult = true; Close(); };
        root.Children.Add(done);
        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = root };
        _updating = false;
        UpdatePreview(initialColor);
    }

    private Border Label(string key, string value, double max, double current)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
        var title = new TextBlock { Text = Localization.T(key), FontSize = 13 * AppSettings.Current.TextScale, VerticalAlignment = VerticalAlignment.Center, Foreground = Brush(Appearance.InkColor) };
        Grid.SetColumn(title, 0);
        grid.Children.Add(title);
        var slider = new Slider { Minimum = 0, Maximum = max, Value = current, IsMoveToPointEnabled = true, IsSnapToTickEnabled = false, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(slider, 1);
        grid.Children.Add(slider);
        var valueText = new TextBlock { Text = value, FontSize = 12 * AppSettings.Current.TextScale, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Foreground = Brush(Appearance.MutedColor) };
        Grid.SetColumn(valueText, 2);
        grid.Children.Add(valueText);
        slider.ValueChanged += (_, _) => valueText.Text = max == 360 ? slider.Value.ToString("0") : $"{slider.Value:P0}";
        return new Border { Child = grid, Tag = slider };
    }

    private void UpdateColorFromSliders()
    {
        if (_updating) return;
        var color = ColorPalette.Hsl(_hue.Value, _saturation.Value, _brightness.Value);
        var value = ColorPalette.ToHex(color);
        _updating = true;
        _hex.Text = value;
        _updating = false;
        UpdatePreview(value);
        _onColorChanged(value);
    }

    private void UpdateColorFromHex()
    {
        if (_updating || _hex.Text.Length != 7) return;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(_hex.Text)!;
            var (hue, saturation, brightness) = ToHsl(color);
            _updating = true;
            _hue.Value = hue;
            _saturation.Value = saturation;
            _brightness.Value = brightness;
            _updating = false;
            UpdatePreview(ColorPalette.ToHex(color));
            _onColorChanged(ColorPalette.ToHex(color));
        }
        catch (FormatException) { }
    }

    private void UpdatePreview(string value)
    {
        try
        {
            _preview.Background = Brush(value);
            _preview.BorderBrush = Brush(Appearance.LineColor);
        }
        catch (FormatException) { }
    }

    private static (double Hue, double Saturation, double Brightness) ToHsl(Color color)
    {
        var r = color.R / 255d; var g = color.G / 255d; var b = color.B / 255d;
        var max = Math.Max(r, Math.Max(g, b)); var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min; var lightness = (max + min) / 2;
        var hue = delta == 0 ? 0 : max == r ? 60 * (((g - b) / delta) % 6) : max == g ? 60 * ((b - r) / delta + 2) : 60 * ((r - g) / delta + 4);
        if (hue < 0) hue += 360;
        var saturation = delta == 0 ? 0 : delta / (1 - Math.Abs(2 * lightness - 1));
        return (hue, saturation, lightness);
    }

    private static Brush Brush(string value) => (Brush)new BrushConverter().ConvertFromString(value)!;
}

internal static class Appearance
{
    private static string Tint(string baseHex, string tintHex, double amount)
    {
        var baseColor = (Color)ColorConverter.ConvertFromString(baseHex)!;
        var tintColor = (Color)ColorConverter.ConvertFromString(tintHex)!;
        byte Mix(byte a, byte b) => (byte)Math.Clamp(Math.Round(a * (1 - amount) + b * amount), 0, 255);
        return $"#{Mix(baseColor.R, tintColor.R):X2}{Mix(baseColor.G, tintColor.G):X2}{Mix(baseColor.B, tintColor.B):X2}";
    }

    public static bool IsDark => AppSettings.Current.Theme == "dark";
    public static string WindowColor => Tint(IsDark ? "#080808" : "#FFFFFF", AppSettings.Current.BackgroundTint, IsDark ? .06 : .18);
    public static string SurfaceColor => Tint(IsDark ? "#191919" : "#FFFFFF", AppSettings.Current.BackgroundTint, IsDark ? .14 : .12);
    public static string HeaderColor => Tint(IsDark ? "#222222" : "#FFFFFF", AppSettings.Current.BackgroundTint, IsDark ? .09 : .30);
    public static string LineColor => Tint(IsDark ? "#3A3A3A" : "#E2E5EA", AppSettings.Current.BackgroundTint, .08);
    public static string InkColor => IsDark ? "#F4F4F4" : "#17191D";
    public static string MutedColor => IsDark ? "#B6B6B6" : "#68717D";
    public static string AccentSurfaceColor => Tint(IsDark ? "#202020" : "#F3F6FB", AppSettings.Current.BackgroundTint, IsDark ? .16 : .10);

    public static void Apply(Window? extraWindow = null)
    {
        var accent = (Brush)new BrushConverter().ConvertFromString(AppSettings.Current.Accent)!;
        if (Application.Current?.MainWindow is MainWindow main)
        {
            main.FlowDirection = AppSettings.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            main.ApplyPreferences();
            WindowsThemeChrome.Attach(main);
        }
        if (extraWindow is not null)
        {
            extraWindow.Background = (Brush)new BrushConverter().ConvertFromString(WindowColor)!;
            extraWindow.FlowDirection = AppSettings.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            WindowsThemeChrome.Attach(extraWindow);
        }
        if (Application.Current is { } app)
        {
            WindowsThemeChrome.UpdateForApplication(app);
            app.Resources["AccentBrush"] = accent;
            app.Resources["SurfaceBrush"] = (Brush)new BrushConverter().ConvertFromString(SurfaceColor)!;
            app.Resources["WindowBrush"] = (Brush)new BrushConverter().ConvertFromString(WindowColor)!;
            app.Resources["InkBrush"] = (Brush)new BrushConverter().ConvertFromString(InkColor)!;
            app.Resources["LineBrush"] = (Brush)new BrushConverter().ConvertFromString(LineColor)!;
            app.Resources["HoverBrush"] = (Brush)new BrushConverter().ConvertFromString(AccentSurfaceColor)!;
            app.Resources["CheckMarkBrush"] = (Brush)new BrushConverter().ConvertFromString(IsDark ? "#FFFFFF" : "#000000")!;
        }
    }
}
