using Microsoft.Extensions.Logging;
using TheRandomizer.Application.Interfaces;
using TheRandomizer.Application.Services;
using TheRandomizer.Maui.ViewModels;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using TheRandomizer.Maui.Services;
using TheRandomizer.Maui.Views;
using TheRandomizer.Maui.Interfaces;

namespace TheRandomizer.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("NotoSans-Regular.ttf", "Sans");
                fonts.AddFont("NotoSans-Bold.ttf", "SansBold");
                fonts.AddFont("NotoSerif-Regular.ttf", "Serif");
                fonts.AddFont("NotoSerif-Bold.ttf", "SerifBold");
                fonts.AddFont("FluentSystemIcons-Filled.ttf", "FluentFilled");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", "Fluent");
                fonts.AddFont("CascadiaCodePL.ttf", "Monospace");
                fonts.AddFont("OpenDyslexic-Regular.otf", "Dyslexic");
                fonts.AddFont("OpenDyslexic-Bold.otf", "DyslexicBold");
            });

#if DEBUG
		builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton(FolderPicker.Default);

#if ANDROID
        builder.Services.AddSingleton<IGeneratorFolderService, AndroidGeneratorFolderService>();
#elif WINDOWS
        builder.Services.AddSingleton<IGeneratorFolderService, WindowsGeneratorFolderService>();
#endif

        builder.Services.AddSingleton<IGeneratorLoader, GeneratorLoader>();
        builder.Services.AddSingleton<IGeneratorRunner, GeneratorRunner>();
        builder.Services.AddSingleton<IGeneratorFileBrowser, GeneratorFileBrowser>();
        builder.Services.AddSingleton<IAppSettingsService, AppSettingService>();
        builder.Services.AddSingleton<IGeneratorManagementService, GeneratorManagementService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ImportGeneratorViewModel>();
        builder.Services.AddTransient<ImportGeneratorPage>();
        builder.Services.AddTransient<GeneratorMetadataEditorViewModel>();
        builder.Services.AddTransient<GeneratorMetadataEditor>();
        builder.Services.AddTransient<AboutPage>();
        builder.Services.AddTransient<AboutViewModel>();

        return builder.Build();
    }
}
