# Localization System Documentation

## Overview

The G2PLC WPF application includes a comprehensive localization system supporting multiple languages. Currently implemented languages:
- **English** (default)
- **Portuguese (Portugal)**

## How to Use

### Switching Languages

1. Launch the application
2. Navigate to **View → Language** in the menu bar
3. Select your preferred language:
   - **English**
   - **Português (Portugal)**

The language will change immediately without requiring an application restart. All UI elements, menus, labels, and dialogs will update to the selected language.

## Technical Implementation

### Resource Files

Language resource dictionaries are located in `src/G2PLC.UI.Wpf/Resources/`:

- **Strings.en.xaml** - English translations
- **Strings.pt.xaml** - Portuguese (Portugal) translations

### Resource Structure

Each language file defines string resources using the following format:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:system="clr-namespace:System;assembly=mscorlib">

    <system:String x:Key="Menu.File">_File</system:String>
    <system:String x:Key="Menu.LoadFile">_Load File</system:String>
    <!-- ... more strings ... -->
</ResourceDictionary>
```

### Key Categories

Strings are organized into logical categories:

| Category | Key Prefix | Example Keys |
|----------|-----------|--------------|
| Menu items | `Menu.*` | `Menu.File`, `Menu.Configuration` |
| Window titles | `Window.*` | `Window.Title` |
| File selection | `FileSelection.*` | `FileSelection.Header`, `FileSelection.Browse` |
| PLC configuration | `PlcConfig.*` | `PlcConfig.Connect`, `PlcConfig.IpAddress` |
| Execution control | `Execution.*` | `Execution.Start`, `Execution.Stop` |
| Status/logging | `Status.*` | `Status.Header`, `Status.Connected` |
| About dialog | `About.*` | `About.Title`, `About.Message` |
| Mappings editor | `Mappings.*` | `Mappings.Title`, `Mappings.Save` |
| Data types | `FileType.*`, `PlcType.*` | `FileType.GCode`, `PlcType.Modbus` |

### Usage in XAML

UI elements reference localized strings using `DynamicResource`:

```xml
<!-- Static text -->
<Label Content="{DynamicResource PlcConfig.IpAddress}"/>

<!-- Window title -->
<Window Title="{DynamicResource Window.Title}">

<!-- Button content -->
<Button Content="{DynamicResource Execution.Start}"/>

<!-- GroupBox header -->
<GroupBox Header="{DynamicResource FileSelection.Header}">
```

### Runtime Language Switching

Language switching is implemented in `MainWindow.xaml.cs`:

```csharp
private void Portuguese_Click(object sender, RoutedEventArgs e)
{
    ApplyLanguage("Resources/Strings.pt.xaml");
    EnglishMenuItem.IsChecked = false;
    PortugueseMenuItem.IsChecked = true;
}

private void ApplyLanguage(string languagePath)
{
    var languageDict = new ResourceDictionary {
        Source = new Uri(languagePath, UriKind.Relative)
    };

    // Replace language dictionary in application resources
    var existingLang = Application.Current.Resources.MergedDictionaries
        .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Resources/Strings.") == true);
    if (existingLang != null)
    {
        var index = Application.Current.Resources.MergedDictionaries.IndexOf(existingLang);
        Application.Current.Resources.MergedDictionaries[index] = languageDict;
    }

    // Update window title
    Title = TryFindResource("Window.Title") as string ?? "G2PLC";
}
```

### Application Startup

The default language (English) is loaded at application startup in `App.xaml.cs`:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // Apply default English language
    var englishStrings = new ResourceDictionary {
        Source = new Uri("Resources/Strings.en.xaml", UriKind.Relative)
    };
    Resources.MergedDictionaries.Add(englishStrings);
    // ...
}
```

## Portuguese (Portugal) Translations

### Key Translation Highlights

| English | Portuguese (Portugal) |
|---------|---------------------|
| File | Ficheiro |
| Load File | Carregar Ficheiro |
| Configuration | Configuração |
| Edit Mappings | Editar Mapeamentos |
| Connect | Ligar |
| Disconnect | Desligar |
| Start | Iniciar |
| Stop | Parar |
| IP Address | Endereço IP |
| Port | Porta |
| Execution Control | Controlo de Execução |
| Status and Log | Estado e Registo |
| Save | Guardar |
| Cancel | Cancelar |
| Register Address | Endereço de Registo |
| Scale Factor | Fator de Escala |

### Complete Translation Coverage

The Portuguese translation includes:
- All menu items (File, Configuration, View, Help)
- All UI labels and buttons
- Dialog boxes (About, Mappings Editor)
- Status messages
- Tooltips and descriptions
- Information text in the Mappings Editor

## Adding a New Language

To add support for a new language:

### 1. Create Resource File

Create a new resource dictionary file in `src/G2PLC.UI.Wpf/Resources/`:
- Naming convention: `Strings.{language-code}.xaml`
- Example: `Strings.es.xaml` for Spanish

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:system="clr-namespace:System;assembly=mscorlib">

    <system:String x:Key="Menu.File">_Archivo</system:String>
    <!-- Copy all keys from Strings.en.xaml and translate -->
</ResourceDictionary>
```

### 2. Add Menu Item

Update `MainWindow.xaml` to add the new language option:

```xml
<MenuItem Header="{DynamicResource Menu.Language}">
    <MenuItem Header="{DynamicResource Menu.English}" Click="English_Click" IsCheckable="True" IsChecked="True" Name="EnglishMenuItem"/>
    <MenuItem Header="{DynamicResource Menu.Portuguese}" Click="Portuguese_Click" IsCheckable="True" Name="PortugueseMenuItem"/>
    <!-- Add new language -->
    <MenuItem Header="Español" Click="Spanish_Click" IsCheckable="True" Name="SpanishMenuItem"/>
</MenuItem>
```

### 3. Add Click Handler

Update `MainWindow.xaml.cs` to handle the language selection:

```csharp
private void Spanish_Click(object sender, RoutedEventArgs e)
{
    ApplyLanguage("Resources/Strings.es.xaml");
    EnglishMenuItem.IsChecked = false;
    PortugueseMenuItem.IsChecked = false;
    SpanishMenuItem.IsChecked = true;
}
```

### 4. Add Language Name Resource

Add the language name to all resource files:

```xml
<!-- Strings.en.xaml -->
<system:String x:Key="Menu.Spanish">Español</system:String>

<!-- Strings.pt.xaml -->
<system:String x:Key="Menu.Spanish">Espanhol</system:String>

<!-- Strings.es.xaml -->
<system:String x:Key="Menu.Spanish">Español</system:String>
```

Then update the menu to use the localized name:

```xml
<MenuItem Header="{DynamicResource Menu.Spanish}" Click="Spanish_Click" IsCheckable="True" Name="SpanishMenuItem"/>
```

## Benefits

✅ **Instant Switching** - Change language without restarting the application
✅ **Complete Coverage** - All UI elements, menus, and dialogs are localized
✅ **Easy Maintenance** - Centralized translation files
✅ **Extensible** - Simple process to add new languages
✅ **DynamicResource Pattern** - Automatic UI updates when language changes
✅ **No Code Changes** - New translations only require XAML resource files

## Translation Keys Reference

### Complete List of Keys

For reference when adding new languages, here are all the translation keys:

```
Menu.File, Menu.LoadFile, Menu.Exit
Menu.Configuration, Menu.EditMappings, Menu.LoadMappings, Menu.SaveMappings
Menu.View, Menu.DarkTheme, Menu.LightTheme, Menu.Language, Menu.English, Menu.Portuguese
Menu.Help, Menu.About

Window.Title

FileSelection.Header, FileSelection.FileType, FileSelection.Browse, FileSelection.Tooltip

PlcConfig.Header, PlcConfig.PlcType, PlcConfig.Connect, PlcConfig.Disconnect
PlcConfig.IpAddress, PlcConfig.Port, PlcConfig.EndpointUrl

Execution.Header, Execution.Start, Execution.Stop, Execution.Line, Execution.Progress

Status.Header, Status.Connected, Status.Disconnected

About.Title, About.Message

Mappings.Title, Mappings.Header, Mappings.Description
Mappings.Axis, Mappings.AxisDescription, Mappings.RegisterAddress, Mappings.ScaleFactor
Mappings.Information, Mappings.Info1, Mappings.Info2, Mappings.Info3
Mappings.Save, Mappings.Cancel

FileType.GCode, FileType.LSF
PlcType.Modbus, PlcType.OpcUa
```

## Best Practices

1. **Use DynamicResource** - Always use `{DynamicResource}` instead of `{StaticResource}` for strings that need to update when language changes

2. **Consistent Key Naming** - Use dot notation with category prefixes (e.g., `Menu.File`, `PlcConfig.Connect`)

3. **Complete Translation** - Ensure all keys from the English file are translated in other language files

4. **Test All Screens** - After adding a language, test all windows and dialogs to verify translations appear correctly

5. **Preserve Formatting** - Keep special characters like underscores for access keys (e.g., `_File` for Alt+F hotkey)

6. **Multiline Text** - For long text like About messages, use `\n` for line breaks (actual newlines, not escaped)

7. **Consistent Terminology** - Use the same translation for the same concept across the entire application
