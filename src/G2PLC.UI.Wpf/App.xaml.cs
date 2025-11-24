using System.Windows;
using G2PLC.Application.Parsers;
using G2PLC.Application.Mappers;
using G2PLC.UI.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WpfApp = System.Windows.Application;

namespace G2PLC.UI.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WpfApp
{
    public IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply default dark theme
        var darkTheme = new ResourceDictionary { Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative) };
        Resources.MergedDictionaries.Add(darkTheme);

        // Apply default English language
        var englishStrings = new ResourceDictionary { Source = new Uri("Resources/Strings.en.xaml", UriKind.Relative) };
        Resources.MergedDictionaries.Add(englishStrings);

        // Configure dependency injection
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        // Create and show main window
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}
