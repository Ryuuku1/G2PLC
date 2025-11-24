using System.Collections.ObjectModel;
using System.Windows;
using G2PLC.Domain.Configuration;
using G2PLC.UI.Wpf.ViewModels;

namespace G2PLC.UI.Wpf;

public partial class MappingsEditorWindow : Window
{
    public MappingConfiguration MappingConfiguration { get; private set; }
    public ObservableCollection<AxisMappingViewModel> AxisMappings { get; }

    public MappingsEditorWindow(MappingConfiguration config)
    {
        InitializeComponent();
        MappingConfiguration = config;
        AxisMappings = new ObservableCollection<AxisMappingViewModel>();

        // Load existing axes
        if (config.RegisterMappings?.Axes != null)
        {
            foreach (var axis in config.RegisterMappings.Axes)
            {
                AxisMappings.Add(new AxisMappingViewModel(axis.Key, axis.Value));
            }
        }

        AxisMappingsControl.ItemsSource = AxisMappings;
        DataContext = this;
    }

    private void AddAxis_Click(object sender, RoutedEventArgs e)
    {
        // Find a default axis name that doesn't exist
        string newAxisName = "NEW";
        int counter = 1;
        while (AxisMappings.Any(a => a.AxisName == newAxisName))
        {
            newAxisName = $"AXIS{counter++}";
        }

        var newAxis = new AxisMappingViewModel(newAxisName, new AxisMappingConfig
        {
            Address = 0,
            ScaleFactor = 1000,
            Description = "New axis",
            AxisType = AxisType.Linear,
            Unit = "mm"
        });

        AxisMappings.Add(newAxis);
    }

    private void RemoveAxis_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.DataContext is AxisMappingViewModel axis)
        {
            AxisMappings.Remove(axis);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Update the configuration with the edited values
        if (MappingConfiguration.RegisterMappings == null)
        {
            MappingConfiguration.RegisterMappings = new RegisterMappings();
        }

        MappingConfiguration.RegisterMappings.Axes = new Dictionary<string, AxisMappingConfig>();
        foreach (var axisVm in AxisMappings)
        {
            MappingConfiguration.RegisterMappings.Axes[axisVm.AxisName] = axisVm.ToConfig();
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
