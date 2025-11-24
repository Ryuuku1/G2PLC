# Theme System Documentation

## Overview

The G2PLC WPF application now includes a comprehensive theming system with both **Dark** and **Light** themes that apply to all UI controls throughout the application.

## How to Use

### Switching Themes

1. Launch the application
2. Navigate to **View** menu in the menu bar
3. Select either:
   - **Dark Theme** - Dark background with light text
   - **Light Theme** - Light background with dark text

The theme will change immediately without requiring an application restart.

## Technical Implementation

### Theme Files

Two theme resource dictionaries are located in `src/G2PLC.UI.Wpf/Themes/`:

- **DarkTheme.xaml** - Dark color scheme (default)
- **LightTheme.xaml** - Light color scheme

### Color Resources

Each theme defines the following color brushes using `DynamicResource` bindings:

| Brush Name | Dark Theme | Light Theme | Usage |
|------------|-----------|-------------|-------|
| BackgroundBrush | #1E1E1E | #F5F5F5 | Window backgrounds, textbox backgrounds |
| SurfaceBrush | #2D2D30 | #FFFFFF | GroupBox backgrounds, surface elements |
| BorderBrush | #3F3F46 | #CCCCCC | Control borders |
| TextBrush | #E0E0E0 | #000000 | Primary text color |
| TextSecondaryBrush | #D4D4D4 | #333333 | Secondary text, log messages |
| PrimaryBrush | #2196F3 | #2196F3 | Primary buttons, highlights |
| PrimaryHoverBrush | #1976D2 | #1976D2 | Button hover states |
| SuccessBrush | #4CAF50 | #4CAF50 | Start button |
| SuccessHoverBrush | #45A049 | #45A049 | Start button hover |
| DangerBrush | #F44336 | #F44336 | Stop button |
| DangerHoverBrush | #DA190B | #DA190B | Stop button hover |

### Styled Controls

All WPF controls are fully themed with custom templates:

- **ComboBox** - Custom template for dropdown with themed popup
- **ComboBoxItem** - Styled items with hover/selection colors
- **Menu/MenuItem** - Custom templates with submenu support
- **Button** - Rounded corners with hover effects
- **TextBox** - Themed backgrounds and caret color
- **GroupBox** - Themed borders and backgrounds
- **Label/TextBlock** - Themed text colors
- **ProgressBar** - Inherits theme colors

### Application-Wide Theme

The theme is applied at the **Application level** in `App.xaml.cs`, ensuring:
- All windows inherit the theme automatically
- Child windows (like MappingsEditorWindow) use the same theme
- Runtime theme switching affects all windows

### Implementation Details

```csharp
// Theme is loaded at application startup (App.xaml.cs)
protected override void OnStartup(StartupEventArgs e)
{
    // Apply default dark theme
    var darkTheme = new ResourceDictionary {
        Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
    };
    Resources.MergedDictionaries.Add(darkTheme);
    // ...
}

// Theme switching in MainWindow.xaml.cs
private void ApplyTheme(string themePath)
{
    var themeDict = new ResourceDictionary {
        Source = new Uri(themePath, UriKind.Relative)
    };

    // Apply to Application resources so all windows inherit
    Application.Current.Resources.MergedDictionaries.Clear();
    Application.Current.Resources.MergedDictionaries.Add(themeDict);

    // Also apply to MainWindow for immediate effect
    Resources.MergedDictionaries.Clear();
    Resources.MergedDictionaries.Add(themeDict);
}
```

## Benefits

✅ **Comprehensive Coverage** - All UI controls properly themed
✅ **Runtime Switching** - Change themes without restart
✅ **Consistent Design** - Unified look across all windows
✅ **Readable Controls** - Proper contrast for ComboBox dropdowns, menus, etc.
✅ **DynamicResource Pattern** - Efficient theme updates
✅ **Extensible** - Easy to add new themes or customize colors

## Adding New Themes

To add a new theme:

1. Create a new ResourceDictionary file in `Themes/` folder (e.g., `BlueTheme.xaml`)
2. Copy the structure from `DarkTheme.xaml` or `LightTheme.xaml`
3. Modify the color values in the `<Color>` resources section
4. Add a menu item in `MainWindow.xaml` for the new theme
5. Add click handler in `MainWindow.xaml.cs` calling `ApplyTheme("Themes/BlueTheme.xaml")`

## Troubleshooting

### Controls Not Themed

If a control doesn't pick up the theme:
1. Ensure it uses `{DynamicResource BrushName}` bindings instead of hardcoded colors
2. Check that the control style is defined in the theme files
3. Verify the theme is loaded at application startup

### Child Windows Not Themed

The MappingsEditorWindow and any future dialog windows will automatically inherit the application-level theme because:
- They don't define their own ResourceDictionary
- They use DynamicResource bindings
- The theme is applied at `Application.Current.Resources` level
