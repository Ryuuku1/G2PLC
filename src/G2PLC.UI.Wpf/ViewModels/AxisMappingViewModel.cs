using CommunityToolkit.Mvvm.ComponentModel;
using G2PLC.Domain.Configuration;

namespace G2PLC.UI.Wpf.ViewModels;

public partial class AxisMappingViewModel : ObservableObject
{
    [ObservableProperty]
    private string _axisName;

    [ObservableProperty]
    private ushort _address;

    [ObservableProperty]
    private decimal _scaleFactor;

    [ObservableProperty]
    private string _description;

    public AxisMappingViewModel(string axisName, AxisMappingConfig config)
    {
        _axisName = axisName;
        _address = config.Address;
        _scaleFactor = config.ScaleFactor;
        _description = config.Description ?? string.Empty;
    }

    public AxisMappingConfig ToConfig()
    {
        return new AxisMappingConfig
        {
            Address = (ushort)Address,
            ScaleFactor = ScaleFactor,
            Description = Description,
            AxisType = AxisType.Linear,
            Unit = "mm"
        };
    }
}
