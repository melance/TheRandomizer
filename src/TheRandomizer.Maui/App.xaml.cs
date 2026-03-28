using TheRandomizer.Application.Interfaces;

namespace TheRandomizer.Maui;

// Todo: Button hover, push, etc
// Todo: Reset settings
// Todo: Generator Maintenance Screen
// Todo: Import/Delete Theme
// Todo: About Screen
// Todo: List Filters

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly AppShell _shell;

    public App(AppShell shell, IAppSettingsService settings, IThemeService themeService)
    {
        InitializeComponent();

        themeService.ApplyTheme(settings.SelectedTheme);

        _shell = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_shell);
    }
}