using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Win32;

namespace TireMark;

internal static class WindowsThemeChrome
{
    private const int DwmUseImmersiveDarkMode = 20;
    private const int LegacyDwmUseImmersiveDarkMode = 19;
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private static readonly ConditionalWeakTable<Window, object> Attached = new();
    private static readonly List<WeakReference<Window>> Windows = [];
    private static readonly object Gate = new();

    static WindowsThemeChrome()
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public static void Attach(Window window)
    {
        lock (Gate)
        {
            if (Attached.TryGetValue(window, out _)) return;
            Attached.Add(window, new object());
            Windows.Add(new WeakReference<Window>(window));
        }

        window.SourceInitialized += (_, _) => ApplyTitleBar(window);
        window.Loaded += (_, _) => ApplyTitleBar(window);
        window.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(ApplyWindowsWheel), true);
        window.Closed += (_, _) =>
        {
            lock (Gate) Attached.Remove(window);
        };
        UpdateScrollBars(Application.Current);
        ApplyTitleBar(window);
    }

    private static void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        var app = Application.Current;
        if (app is null || app.Dispatcher.HasShutdownStarted) return;
        app.Dispatcher.BeginInvoke(() =>
        {
            UpdateScrollBars(app);
            List<Window> open = [];
            lock (Gate)
            {
                for (var i = Windows.Count - 1; i >= 0; i--)
                {
                    if (Windows[i].TryGetTarget(out var window) && window.IsLoaded) open.Add(window);
                    else Windows.RemoveAt(i);
                }
            }
            foreach (var window in open) ApplyTitleBar(window);
        });
    }

    private static bool IsWindowsDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            var value = key?.GetValue("AppsUseLightTheme") ?? key?.GetValue("SystemUsesLightTheme");
            return value is int lightTheme && lightTheme == 0;
        }
        catch { return false; }
    }

    private static void ApplyTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        var dark = IsWindowsDarkTheme() ? 1 : 0;
        var result = DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref dark, sizeof(int));
        if (result != 0) _ = DwmSetWindowAttribute(handle, LegacyDwmUseImmersiveDarkMode, ref dark, sizeof(int));
    }

    private static void UpdateScrollBars(Application? app)
    {
        if (app is null || app.Dispatcher.HasShutdownStarted) return;
        var dark = IsWindowsDarkTheme();
        app.Resources["WindowsScrollTrackBrush"] = FrozenBrush(dark ? "#242424" : "#F1F1F1");
        app.Resources["WindowsScrollThumbBrush"] = FrozenBrush(dark ? "#555555" : "#BDBDBD");
        app.Resources["WindowsScrollThumbHoverBrush"] = FrozenBrush(dark ? "#777777" : "#969696");
        if (app.Resources.Contains(typeof(ScrollBar))) return;

        var style = new Style(typeof(ScrollBar));
        style.Setters.Add(new Setter(FrameworkElement.WidthProperty, 14d));
        style.Setters.Add(new Setter(FrameworkElement.HeightProperty, double.NaN));
        style.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension("WindowsScrollTrackBrush")));
        style.Setters.Add(new Setter(Control.TemplateProperty, CreateScrollBarTemplate()));
        var horizontal = new Trigger { Property = ScrollBar.OrientationProperty, Value = Orientation.Horizontal };
        horizontal.Setters.Add(new Setter(FrameworkElement.WidthProperty, double.NaN));
        horizontal.Setters.Add(new Setter(FrameworkElement.HeightProperty, 14d));
        style.Triggers.Add(horizontal);
        app.Resources[typeof(ScrollBar)] = style;
    }

    private static void ApplyWindowsWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || e.OriginalSource is not DependencyObject source) return;
        var viewer = FindScrollViewer(source);
        if (viewer is null) return;

        var lines = SystemParameters.WheelScrollLines;
        if (lines == 0) return;
        var direction = e.Delta > 0 ? -1 : 1;
        if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0)
        {
            if (lines < 0) { if (direction < 0) viewer.LineLeft(); else viewer.LineRight(); }
            else for (var i = 0; i < Math.Clamp(lines, 1, 20); i++) { if (direction < 0) viewer.LineLeft(); else viewer.LineRight(); }
        }
        else if (lines < 0)
        {
            if (direction < 0) viewer.PageUp(); else viewer.PageDown();
        }
        else
        {
            for (var i = 0; i < Math.Clamp(lines, 1, 20); i++) { if (direction < 0) viewer.LineUp(); else viewer.LineDown(); }
        }
        e.Handled = true;
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject? node)
    {
        while (node is not null)
        {
            if (node is ScrollViewer viewer) return viewer;
            DependencyObject? parent = null;
            try
            {
                if (node is Visual or Visual3D) parent = VisualTreeHelper.GetParent(node);
            }
            catch { }
            node = parent ?? LogicalTreeHelper.GetParent(node);
        }
        return null;
    }

    public static void UpdateForApplication(Application app) => UpdateScrollBars(app);

    private static ControlTemplate CreateScrollBarTemplate() => (ControlTemplate)XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         TargetType="{x:Type ScrollBar}">
          <Border Background="{DynamicResource WindowsScrollTrackBrush}" CornerRadius="7">
            <Track x:Name="PART_Track"
                   Orientation="{TemplateBinding Orientation}"
                   IsDirectionReversed="True"
                   Minimum="{TemplateBinding Minimum}"
                   Maximum="{TemplateBinding Maximum}"
                   Value="{TemplateBinding Value}"
                   ViewportSize="{TemplateBinding ViewportSize}">
              <Track.DecreaseRepeatButton>
                <RepeatButton Command="{x:Static ScrollBar.PageUpCommand}"
                              Opacity="0" Focusable="False" IsTabStop="False" />
              </Track.DecreaseRepeatButton>
              <Track.Thumb>
                <Thumb x:Name="ScrollThumb" Focusable="False">
                  <Thumb.Template>
                    <ControlTemplate TargetType="Thumb">
                      <Border x:Name="ThumbFace" Background="{DynamicResource WindowsScrollThumbBrush}"
                              CornerRadius="7" Margin="2" />
                      <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                          <Setter TargetName="ThumbFace" Property="Background"
                                  Value="{DynamicResource WindowsScrollThumbHoverBrush}" />
                        </Trigger>
                      </ControlTemplate.Triggers>
                    </ControlTemplate>
                  </Thumb.Template>
                </Thumb>
              </Track.Thumb>
              <Track.IncreaseRepeatButton>
                <RepeatButton Command="{x:Static ScrollBar.PageDownCommand}"
                              Opacity="0" Focusable="False" IsTabStop="False" />
              </Track.IncreaseRepeatButton>
            </Track>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="Orientation" Value="Horizontal">
              <Setter TargetName="PART_Track" Property="IsDirectionReversed" Value="False" />
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
        """);

    private static SolidColorBrush FrozenBrush(string color)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
        brush.Freeze();
        return brush;
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);
}
