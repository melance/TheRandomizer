using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheRandomizer.Application.Enumerators;
using TheRandomizer.Application.Interfaces;
using TheRandomizer.Application.Models;
using TheRandomizer.Maui.Services;

namespace TheRandomizer.Maui.ViewModels;

public sealed record FontOption(String Name, String Value);
public sealed record FontSizeOption(String Name, FontSizes Value);

public partial class SettingsViewModel(IGeneratorFolderService folderService,
                                       IAppSettingsService settings,
                                       IThemeService themeService) : ObservableObject
{
    private readonly IGeneratorFolderService _folderService = folderService;
    private readonly IAppSettingsService _settings = settings;
    private readonly IThemeService _themeService = themeService;

    public IReadOnlyList<FontOption> FontOptions { get; } =
    [
        new("Sans Serif", "Sans"),
        new("Serif", "Serif"),
        new("Monospace", "Monospace"),
        new("Dyslexic", "Dyslexic")
    ];

    public IReadOnlyList<FontSizeOption> FontSizeOptions { get; } =
    [
        new("Small", FontSizes.Small),
        new("Medium", FontSizes.Medium),
        new("Large", FontSizes.Large),
        new("Extra Large", FontSizes.ExtraLarge)
    ];

    public IReadOnlyList<AppThemeSetting> AppThemeOptions { get; } =
        Enum.GetValues<AppThemeSetting>();

    public Boolean CanBrowseFolder => _folderService.CanBrowse;

    [ObservableProperty]
    public partial String GeneratorFolder { get; set; } = String.Empty;

    [ObservableProperty]
    public partial AppThemeSetting AppTheme { get; set; } = AppThemeSetting.System;

    [ObservableProperty]
    public partial ThemeDefinition SelectedTheme { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ThemeDefinition> Themes { get; set; } = themeService.GetThemesAsync().Result;

    [ObservableProperty]
    public partial FontOption FontFamily { get; set; }

    [ObservableProperty]
    public partial FontSizeOption FontSize { get; set; }

    [RelayCommand]
    private async Task PickGeneratorFolderAsync()
    {
        if (_folderService.CanBrowse)
        {
            var folder = await _folderService.BrowseAsync();

            if (!String.IsNullOrWhiteSpace(folder))
                GeneratorFolder = folder;
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        GeneratorFolder = await _folderService.GetCurrentFolderAsync();
        SelectedTheme = Themes.FirstOrDefault(t => t.Id.Equals(_settings.SelectedTheme, StringComparison.OrdinalIgnoreCase))
                        ?? ThemeService.DefaultTheme;
        FontSize = FontSizeOptions.FirstOrDefault(x => x.Value == _settings.FontSize) ?? FontSizeOptions[1];
        FontFamily = FontOptions.FirstOrDefault(x => x.Value == _settings.FontFamily) ?? FontOptions[0];
        AppTheme = _settings.AppTheme;
    }

    [RelayCommand]
    public void Reset()
    {
        GeneratorFolder = Path.Combine(FileSystem.AppDataDirectory, "Definitions");
        AppTheme = AppThemeSetting.System;
        SelectedTheme = ThemeService.DefaultTheme;
        FontSize = FontSizeOptions[1];
        FontFamily = FontOptions[0];
    }

    [RelayCommand]
    private void Save()
    {
        if (_folderService.CanBrowse)
            _settings.GeneratorFolder = GeneratorFolder;

        _settings.SelectedTheme = SelectedTheme.Id;
        _settings.FontSize = FontSize.Value;
        _settings.FontFamily = FontFamily.Value;
        _settings.AppTheme = AppTheme;
        _themeService.ApplyTheme(SelectedTheme.Id);
    }

}

