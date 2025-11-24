using System.Windows;
using G2PLC.Domain.Configuration;

namespace G2PLC.UI.Wpf;

public partial class MappingsEditorWindow : Window
{
    public MappingConfiguration MappingConfiguration { get; private set; }

    public MappingsEditorWindow(MappingConfiguration config)
    {
        InitializeComponent();
        MappingConfiguration = config;

        // Bind the axes to the ItemsControl
        if (config.RegisterMappings?.Axes != null)
        {
            AxisMappingsControl.ItemsSource = config.RegisterMappings.Axes;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
