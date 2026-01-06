# DesktopClock - Workspace Instructions for GitHub Copilot

## Project Overview
DesktopClock is a lightweight desktop clock application built with WPF and .NET. It provides an always-visible clock on the user's desktop with extensive customization options.

## GitHub Copilot Agent Behavior

### Response Style
- **Explain before changing**: When asked diagnostic questions ("why isn't this working?", "what's wrong here?"), provide an explanation first
- **Ask for confirmation** before making changes if the request is ambiguous
- Questions starting with "why", "how", "what" should get explanations, not automatic fixes

## Technology Stack
- **Framework**: .NET with WPF (Windows Presentation Foundation)
- **UI**: XAML for markup, C# for code-behind
- **Additional**: Windows Forms integration for certain features
- **Build**: MSBuild/Visual Studio solution

## Key Dependencies
- **CommunityToolkit.Mvvm** (v8.4.0) - MVVM patterns and helpers
- **H.NotifyIcon.Wpf** (v2.2.0) - System tray integration
- **Humanizer.Core** (v2.14.1) - String manipulation and formatting
- **Microsoft.Office.Interop.Outlook** (v15.0.4797.1003) - Outlook calendar integration via NuGet
- **Newtonsoft.Json** (v13.0.3) - JSON serialization
- **Wacton.Unicolour** (v6.3.0) - Color manipulation

## Project Structure

### Core Components
- **DesktopClock/** - Main application project
  - `App.xaml` / `App.xaml.cs` - Application entry point and startup configuration
  - `MainWindow.xaml` / `MainWindow.xaml.cs` - Primary window implementation
  
- **Data/** - Data services and timers
  - `SystemClockTimer.cs` - Clock timer implementation
  - `OutlookCalendarService.cs` - Outlook calendar integration
  
- **Modules/** - Modular functionality
  - `IWindowModule.cs` - Module interface
  - `ClockModule.cs` - Clock display module
  - `BackgroundColorModule.cs` - Background customization
  - `TextColorModule.cs` - Text color customization
  
- **Properties/** - Application settings
  - `Settings.cs` - User settings management
  
- **Utilities/** - Helper utilities

- **DesktopClock.Tests/** - Unit tests project
  - `SystemClockTests.cs` - Clock timer tests
  - `TokenizerTests.cs` - Format tokenizer tests

## Coding Guidelines

### C# Conventions
- Use **file-scoped namespaces** (e.g., `namespace DesktopClock;`)
- Follow **standard C# naming conventions**:
  - PascalCase for public members, methods, and classes
  - camelCase for local variables and parameters
  - _camelCase for private fields (if used)
- Prefer **explicit types** over `var` when the type isn't obvious
- Use **XML documentation comments** for public APIs

### XAML Conventions
- Follow the formatting rules defined in `Settings.XamlStyler`
- Keep code-behind minimal; prefer MVVM patterns where applicable
- Use data binding for dynamic content

### Architecture Patterns
- **Module Pattern**: New features should implement `IWindowModule` interface
- **MVVM**: Use CommunityToolkit.Mvvm for ViewModels and commands
- **Settings Management**: Persist user preferences through the Properties/Settings system
- **Separation of Concerns**: Keep UI (XAML), logic (C#), and data (services) separated

### Testing
- Write unit tests for new functionality in `DesktopClock.Tests`
- Test clock timing logic, formatting, and data transformations
- Use descriptive test method names following the pattern: `MethodName_Scenario_ExpectedBehavior`

## Development Workflow

### Building
- **IMPORTANT**: Use `msbuild` to build the solution (or Visual Studio if building manually)
- `dotnet build` will NOT work due to COM Interop dependencies (Office Interop references)
- The COM references require MSBuild's traditional project system
- Build command: `msbuild DesktopClock.sln` or `msbuild DesktopClock.sln /p:Configuration=Release`
- **Warnings are treated as errors in Release builds** - ensure code is warning-free
- The project builds to `bin/Debug` or `bin/Release`

### Running
- Execute `.\runclock.ps1` to build, publish, and launch the application
- The script stops any existing DesktopClock process, publishes to `~/tools`, and runs the app
- The app runs as a desktop overlay window

### Project Notes
- This is a personal "just for me" project
- Run and test using `runclock.ps1` for the full workflow

### Key Features to Maintain
- Always-on-top window behavior
- Customizable clock formats
- Background and text color customization
- System tray integration
- Run on startup capability
- Outlook calendar integration
- Lightweight and performant

## Special Considerations

### COM Interop
- Outlook integration uses COM interop - be careful with object lifecycle
- Always release COM objects properly to avoid memory leaks

### Windows Integration
- Application uses Windows Forms alongside WPF for certain features
- Registry manipulation for run-on-startup feature
- System tray icon management

### Performance
- Clock updates frequently - optimize timer callbacks
- Minimize allocations in hot paths
- Be mindful of XAML data binding performance

## Code Style Notes
- Keep methods focused and single-purpose
- Use meaningful variable names
- Prefer composition over inheritance for modules
- Handle exceptions gracefully, especially for Outlook/COM failures
- Log errors appropriately for debugging

## When Adding New Features
1. Consider if it should be a module implementing `IWindowModule`
2. Add corresponding settings if user-configurable
3. Update tests in `DesktopClock.Tests`
4. Ensure backward compatibility with existing settings
5. Document public APIs with XML comments
6. Test on both Debug and Release builds

## Common Tasks
- **Adding a new module**: Implement `IWindowModule`, register in app initialization
- **Adding settings**: Update `Properties/Settings.cs` and UI
- **Modifying clock display**: Work with `ClockModule.cs` and XAML bindings
- **Calendar features**: Extend `OutlookCalendarService.cs`
- **Theming**: Update color modules (`BackgroundColorModule.cs`, `TextColorModule.cs`)

## Author & License
- Original Author: Daniel Chalmers
- Current Branch: `outlook` (Outlook integration features)
- License: Check `LICENSE` file for details
