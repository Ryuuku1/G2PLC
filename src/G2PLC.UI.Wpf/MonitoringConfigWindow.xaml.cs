using System.Collections.ObjectModel;
using System.Windows;
using G2PLC.UI.Wpf.ViewModels;

namespace G2PLC.UI.Wpf;

public partial class MonitoringConfigWindow : Window
{
    public ObservableCollection<RegisterMonitorViewModel> MonitoredRegisters { get; }

    public MonitoringConfigWindow(ObservableCollection<RegisterMonitorViewModel> registers)
    {
        InitializeComponent();

        // Create a copy of the registers so we don't modify the original until Save is clicked
        MonitoredRegisters = new ObservableCollection<RegisterMonitorViewModel>();
        foreach (var reg in registers)
        {
            var copy = new RegisterMonitorViewModel(reg.Address, reg.Description);
            copy.CurrentValue = reg.CurrentValue;
            MonitoredRegisters.Add(copy);
        }

        DataContext = this;
    }

    private void AddRegister_Click(object sender, RoutedEventArgs e)
    {
        // Find next available address
        ushort newAddress = 0;
        while (MonitoredRegisters.Any(r => r.Address == newAddress))
        {
            newAddress++;
        }

        MonitoredRegisters.Add(new RegisterMonitorViewModel(newAddress, $"Register {newAddress}"));
    }

    private void RemoveRegister_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.DataContext is RegisterMonitorViewModel register)
        {
            MonitoredRegisters.Remove(register);
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
