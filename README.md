```
    ____  __            _           
   / __ \/ /_  ______ _(_)___  _____
  / /_/ / / / / / __ `/ / __ \/ ___/
 / ____/ / /_/ / /_/ / / / / (__  ) 
/_/   /_/\__,_/\__, /_/_/ /_/____/  
              /____/                
```

# Mythetech.Plugins

This repository contains example plugins and a desktop application for testing plugins built with the Mythetech.Framework.

## Technology Stack

- **.NET 10** - Built using .NET 10 for the latest features and performance improvements
- **Razor Class Libraries** - Utilizes Razor Class Libraries to create reusable UI components and plugin modules

## Mythetech.Framework

This project integrates the [Mythetech.Framework](https://github.com/Mythetech/Mythetech.Framework) package, which provides the core infrastructure for building plugins. The framework includes:

- Essential plugin interfaces and base classes
- Plugin discovery and loading mechanisms
- Storage and messaging infrastructure
- A sample plugin demonstrating best practices

For more information, see the [Mythetech.Framework repository](https://github.com/Mythetech/Mythetech.Framework).

## Desktop Application

This repository includes a desktop application (`Mythetech.Plugins.PluginTester`) located in the `tools/` directory. The PluginTester application is designed to help developers test and debug plugins during development. It provides:

- A user-friendly interface to load and test plugins
- Plugin management capabilities
- Integration with the plugin framework for real-world testing scenarios

## Building Plugins

Plugins for the Mythetech framework are built as Razor Class Libraries targeting .NET 10. There are two types of plugin components you can create:

### PluginContextPanel

PluginContextPanel components are main UI components that appear in the application's context panel. These are full-featured panels that provide the primary functionality of your plugin.

**Example Structure:**

```csharp
@using Mythetech.Framework.Infrastructure.Plugins.Components
@inherits PluginContextPanel
@attribute [PluginComponentMetadata(
    Icon = Icons.Material.Outlined.YourIcon,
    Title = "Your Plugin Name",
    Order = 10)]

@code {
    public override string Icon { get; } = Icons.Material.Outlined.YourIcon;
    public override string Title { get; } = "Your Plugin Name";
    
    // Your component logic here
}
```

**Requirements:**
- Inherit from `Mythetech.Framework.Infrastructure.Plugins.Components.PluginContextPanel`
- Use the `[PluginComponentMetadata]` attribute to specify icon, title, and display order
- Override the `Icon` and `Title` properties

**Examples in this repository:**
- `src/Mythetech.Plugins.Notes/NotePanel.razor` - A notes editor panel
- `src/Mythetech.Plugins.Games/GamesPanel.razor` - A games selection panel

### PluginMenu

PluginMenu components provide menu items and toolbar actions that integrate into the application's menu system. These are typically used for actions and commands related to your plugin.

**Example Structure:**

```csharp
@using Mythetech.Framework.Infrastructure.Plugins.Components
@inherits PluginMenu
@attribute [PluginComponentMetadata(
    Icon = Icons.Material.Outlined.YourIcon,
    Title = "Your Plugin Name",
    Order = 10)]

@code {
    public override string Icon { get; } = Icons.Material.Outlined.YourIcon;
    public override string Title { get; } = "Your Plugin Name";
    
    public override PluginMenuItem[] Items =>
    [
        new()
        {
            Text = "Action Name",
            OnClick = (async (_) => await YourAction()),
            Icon = Icons.Material.Outlined.ActionIcon
        },
    ];
}
```

**Requirements:**
- Inherit from `Mythetech.Framework.Infrastructure.Plugins.Components.PluginMenu`
- Use the `[PluginComponentMetadata]` attribute
- Override the `Icon`, `Title`, and `Items` properties
- Provide an array of `PluginMenuItem` objects with text, click handlers, and icons

**Example in this repository:**
- `src/Mythetech.Plugins.Notes/NoteExportToolbar.razor` - Export functionality for notes

### Plugin Manifest

All plugins must include a `Manifest` class that implements `IPluginManifest`. This class provides metadata about your plugin.

**Example Manifest:**

```csharp
using Mythetech.Framework.Infrastructure.Plugins;

public class Manifest : IPluginManifest
{
    public string Id { get; } = "YourCompany.Plugins.YourPlugin";
    public string Name { get; } = "Your Plugin Name";
    public Version Version { get; } = new Version(1, 0, 0);
    public string Developer { get; } = "Your Name";
    public string Description { get; } = "Description of your plugin";
    public string? Icon { get; } = Icons.Material.Outlined.YourIcon;
    public PluginAsset[] Assets => []; // Optional: CSS/JS assets
}
```

### Project Setup

1. Create a new Razor Class Library project targeting .NET 10.0
2. Add a reference to the `Mythetech.Framework` NuGet package
3. Create your plugin components (PluginContextPanel and/or PluginMenu)
4. Create a `Manifest` class implementing `IPluginManifest`
5. Build your project as a Razor Class Library

**Example .csproj:**

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Mythetech.Framework" Version="0.2.1" />
    </ItemGroup>
</Project>
```

## Examples

This repository includes two example plugins:

- **Mythetech.Plugins.Notes** - A note-taking plugin that demonstrates both PluginContextPanel and PluginMenu components
- **Mythetech.Plugins.Games** - A games plugin that demonstrates PluginContextPanel with dynamic component loading

These examples can serve as reference implementations when building your own plugins.

