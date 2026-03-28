using System.Runtime.CompilerServices;
using TheRandomizer.Application.Enumerators;
using TheRandomizer.Application.Interfaces;

namespace TheRandomizer.Maui.Services;

internal class AppSettingService : IAppSettingsService
{
    public String GeneratorFolder
    {
        get => Get(String.Empty);
        set => Set(value);
    }

    public AppThemeSetting AppTheme
    {
        get => Enum.Parse<AppThemeSetting>(Get(AppThemeSetting.System.ToString()));
        set => Set(value.ToString());
    }

    public String SelectedTheme 
    { 
        get => Get(String.Empty); 
        set => Set(value); 
    }

    public String FontFamily 
    { 
        get => Get("Sans");
        set => Set(value); 
    }

    public FontSizes FontSize
    {
        get => Enum.Parse<FontSizes>(Get(FontSizes.Medium.ToString()));
        set => Set(value.ToString());
    }

    private static T Get<T>(T defaultValue, [CallerMemberName] String key = "")
        => Preferences.Default.Get(key, defaultValue);

    private static void Set(String? value, [CallerMemberName] String key = "") 
        => Preferences.Set(key, value);
}

