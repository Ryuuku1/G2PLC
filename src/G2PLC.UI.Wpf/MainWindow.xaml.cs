using System.IO;
using System.Windows;
using G2PLC.UI.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using WpfApp = System.Windows.Application;

namespace G2PLC.UI.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Set DataContext to MainViewModel
        var viewModel = ((App)WpfApp.Current).Services.GetRequiredService<MainViewModel>();
        DataContext = viewModel;
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        WpfApp.Current.Shutdown();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var message = TryFindResource("About.Message") as string ?? "G2PLC - CNC to PLC Controller\n\nVersion 1.0";
        var title = TryFindResource("About.Title") as string ?? "About G2PLC";

        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void DarkTheme_Click(object sender, RoutedEventArgs e)
    {
        ApplyTheme("Themes/DarkTheme.xaml");
        DarkThemeMenuItem.IsChecked = true;
        LightThemeMenuItem.IsChecked = false;
    }

    private void LightTheme_Click(object sender, RoutedEventArgs e)
    {
        ApplyTheme("Themes/LightTheme.xaml");
        DarkThemeMenuItem.IsChecked = false;
        LightThemeMenuItem.IsChecked = true;
    }

    private void English_Click(object sender, RoutedEventArgs e)
    {
        ApplyLanguage("Resources/Strings.en.xaml");
        EnglishMenuItem.IsChecked = true;
        PortugueseMenuItem.IsChecked = false;
    }

    private void Portuguese_Click(object sender, RoutedEventArgs e)
    {
        ApplyLanguage("Resources/Strings.pt.xaml");
        EnglishMenuItem.IsChecked = false;
        PortugueseMenuItem.IsChecked = true;
    }

    private void OpenExamplesFolder_Click(object sender, RoutedEventArgs e)
    {
        var examplesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "examples");

        // Create directory if it doesn't exist and copy example files
        if (!Directory.Exists(examplesPath))
        {
            Directory.CreateDirectory(examplesPath);
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = examplesPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not open examples folder: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenSimple3AxisExample_Click(object sender, RoutedEventArgs e)
    {
        OpenExampleFile("mappings_simple_3axis.json");
    }

    private void OpenModbusExample_Click(object sender, RoutedEventArgs e)
    {
        OpenExampleFile("mappings_modbus_example.json");
    }

    private void OpenOpcUaExample_Click(object sender, RoutedEventArgs e)
    {
        OpenExampleFile("mappings_opcua_example.json");
    }

    private void OpenExampleFile(string fileName)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "examples", fileName);

        if (!File.Exists(filePath))
        {
            MessageBox.Show(
                $"Example file not found: {fileName}\n\nPath: {filePath}",
                "File Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not open file: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ApplyTheme(string themePath)
    {
        var themeDict = new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };

        // Find and replace theme dictionary in application resources
        var existingTheme = WpfApp.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Themes/") == true);
        if (existingTheme != null)
        {
            var index = WpfApp.Current.Resources.MergedDictionaries.IndexOf(existingTheme);
            WpfApp.Current.Resources.MergedDictionaries[index] = themeDict;
        }
        else
        {
            WpfApp.Current.Resources.MergedDictionaries.Add(themeDict);
        }

        // Also update MainWindow resources
        var existingThemeLocal = Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Themes/") == true);
        if (existingThemeLocal != null)
        {
            var index = Resources.MergedDictionaries.IndexOf(existingThemeLocal);
            Resources.MergedDictionaries[index] = themeDict;
        }
    }

    private void ApplyLanguage(string languagePath)
    {
        var languageDict = new ResourceDictionary { Source = new Uri(languagePath, UriKind.Relative) };

        // Find and replace language dictionary in application resources
        var existingLang = WpfApp.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Resources/Strings.") == true);
        if (existingLang != null)
        {
            var index = WpfApp.Current.Resources.MergedDictionaries.IndexOf(existingLang);
            WpfApp.Current.Resources.MergedDictionaries[index] = languageDict;
        }
        else
        {
            WpfApp.Current.Resources.MergedDictionaries.Add(languageDict);
        }

        // Also update MainWindow resources
        var existingLangLocal = Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Resources/Strings.") == true);
        if (existingLangLocal != null)
        {
            var index = Resources.MergedDictionaries.IndexOf(existingLangLocal);
            Resources.MergedDictionaries[index] = languageDict;
        }

        // Update window title
        Title = TryFindResource("Window.Title") as string ?? "G2PLC - CNC to PLC Controller";
    }
}
