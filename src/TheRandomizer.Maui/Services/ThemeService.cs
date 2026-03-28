using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using TheRandomizer.Application.Enumerators;
using TheRandomizer.Application.Interfaces;
using TheRandomizer.Application.Models;
using TheRandomizer.Application.Models.Results;

namespace TheRandomizer.Maui.Services;

public sealed class ThemeService : IThemeService
{

    #region Constructor
    public ThemeService(IAppSettingsService settings)
    {
        _settings = settings;
        Directory.CreateDirectory(ThemesFolder);
    }
    #endregion

    #region Constants
    private const String DEFAULT_THEME_ID = "default";
    #endregion

    #region Members
    private readonly IAppSettingsService _settings;
    private static readonly Dictionary<String, String> DefaultColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Primary"] = "#512BD4",
        ["PrimaryDark"] = "#ac99ea",
        ["PrimaryDarkText"] = "#242424",
        ["Secondary"] = "#DFD8F7",
        ["SecondaryDarkText"] = "#9880e5",
        ["Tertiary"] = "#2B0B98",

        ["White"] = "#FFFFFF",
        ["Black"] = "#000000",
        ["Magenta"] = "#D600AA",
        ["MidnightBlue"] = "#190649",
        ["OffBlack"] = "#1f1f1f",
        ["Danger"] = "#A1222F",
        ["DangerDark"] = "#DA2C43",

        ["Gray100"] = "#E1E1E1",
        ["Gray200"] = "#C8C8C8",
        ["Gray300"] = "#ACACAC",
        ["Gray400"] = "#919191",
        ["Gray500"] = "#6E6E6E",
        ["Gray600"] = "#404040",
        ["Gray900"] = "#212121",
        ["Gray950"] = "#141414"
    };

    private static readonly String[] BrushKeys =
    [
        "Primary",
        "Secondary",
        "Tertiary",
        "White",
        "Black",
        "Gray100",
        "Gray200",
        "Gray300",
        "Gray400",
        "Gray500",
        "Gray600",
        "Gray900",
        "Gray950"
    ];
    public static readonly ThemeDefinition DefaultTheme = new()
    {
        Id = DEFAULT_THEME_ID,
        Name = "Default",
        Author = "Built In",
        IsBuiltIn = true,
        Colors = new Dictionary<string, string>(DefaultColors!, StringComparer.OrdinalIgnoreCase)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
    #endregion

    #region Properties
    public static String DefaultThemeId => DEFAULT_THEME_ID;
    public String ThemesFolder => Path.Combine(FileSystem.Current.AppDataDirectory, "Themes");
    #endregion

    #region Public Methods
    public void ReapplyTheme()
    {
        ApplyTheme(_settings.SelectedTheme);
    }

    public void ApplyTheme(String themeId)
    {
        var merged = DefaultTheme.Copy();

        if (!String.IsNullOrWhiteSpace(themeId) &&
            !themeId.Equals(DEFAULT_THEME_ID, StringComparison.OrdinalIgnoreCase))
        {
            var theme = GetThemeAsync(themeId).Result;

            if (theme is not null)
            {
                foreach (var pair in theme.Colors)
                    merged.Colors[pair.Key] = pair.Value;
            }
        }

        Microsoft.Maui.Controls.Application.Current?.UserAppTheme = GetAppTheme();
        ApplyMergedTheme(merged);
    }

    public async Task DeleteThemeAsync(String themeId)
    {
        if (!String.IsNullOrWhiteSpace(themeId) &&
            !themeId.Equals(DEFAULT_THEME_ID, StringComparison.OrdinalIgnoreCase))
        {
            var theme = await LoadThemeFileAsync(themeId);

            if (theme is not null)
                File.Delete(theme.Path);
        }
    }

    public async Task<ThemeDefinition?> GetThemeAsync(String themeId)
    {
        if (!String.IsNullOrWhiteSpace(themeId))
        {
            if (themeId.Equals(DEFAULT_THEME_ID, StringComparison.OrdinalIgnoreCase))
            {
                return DefaultTheme;
            }

            foreach(var theme in await GetThemesAsync())
            {
                if (theme.Id.Equals(themeId, StringComparison.OrdinalIgnoreCase))
                    return theme;
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<ThemeDefinition>> GetThemesAsync()
    {
        var themes = new List<ThemeDefinition>()
        {
            DefaultTheme
        };

        foreach (var path in Directory.EnumerateFiles(ThemesFolder, "*.theme.json", SearchOption.TopDirectoryOnly))
        {
            var theme = await LoadThemeFileAsync(path);
            if (theme is not null)
            {
                theme.Path = path;
                themes.Add(theme);
            }
        }

        return [.. themes
                   .OrderBy(t => t.IsBuiltIn ? 0 : 1)
                   .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<ImportThemeResult> ImportThemeAsync(String sourceFilePath, Boolean overwrite = false)
    {
        try
        {
            if (!String.IsNullOrWhiteSpace(sourceFilePath) && File.Exists(sourceFilePath))
            {
                var theme = await LoadThemeFileAsync(sourceFilePath);
                if (theme is not null && !theme.IsBuiltIn)
                {
                    var destinationPath = Path.Combine(ThemesFolder, sourceFilePath);

                    if (!File.Exists(destinationPath) || overwrite)
                    {
                        File.Copy(sourceFilePath, destinationPath, overwrite);
                        return new() { Success = true };
                    }
                    else
                    {
                        return BaseGeneratorResult.Warning<ImportThemeResult>("A theme with the same file name already exist.");
                    }
                }
            }
            return BaseGeneratorResult.Error<ImportThemeResult>($"Unable to import theme file {sourceFilePath}.");
        }
        catch(Exception ex)
        {
            return BaseGeneratorResult.Exception<ImportThemeResult>(ex);
        }
    }
    #endregion

    #region Private Methods
    private void ApplyMergedTheme(ThemeDefinition theme)
    {
        var resources = Microsoft.Maui.Controls.Application.Current?.Resources;
        if (resources is not null)
        {
            foreach(var pair in theme.Colors)
            {
                if (TryParseColor(pair.Value, out var color))
                {
                    resources[pair.Key] = color;
                }
            }

            var (body, title, small) = Utilities.TextSizeMap.Get(_settings.FontSize);

            resources["AppFontFamily"] = _settings.FontFamily;
            resources["AppFontSize"] = body;
            resources["AppTitleFontSize"] = title;
            resources["AppSmallFontSize"] = small;

            RebuildBrushes(resources);
        }
    }

    private static Boolean IsValidTheme([NotNullWhen(true)] ThemeDefinition? theme)
    {
        if (theme == null) return false;
        if (String.IsNullOrWhiteSpace(theme.Id)) return false;
        if (String.IsNullOrWhiteSpace(theme.Name)) return false;
        if (theme.Colors.Count == 0) return false;
        return true;
    }

    private static async Task<ThemeDefinition?> LoadThemeFileAsync(String path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var theme = JsonSerializer.Deserialize<ThemeDefinition>(json, JsonOptions);

            if (IsValidTheme(theme))
            {
                var colors = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

                if (theme.Colors is not null)
                {
                    foreach (var pair in theme.Colors)
                    {
                        if (!String.IsNullOrWhiteSpace(pair.Key) &&
                            !String.IsNullOrWhiteSpace(pair.Value) &&
                            DefaultColors.ContainsKey(pair.Key) &&
                            TryParseColor(pair.Value, out _))
                        {
                            colors[pair.Key] = pair.Value;
                        }
                    }
                }
                return new()
                {
                    Id = theme.Id.Trim(),
                    Name = theme.Name.Trim(),
                    Author = String.IsNullOrWhiteSpace(theme.Author) ? String.Empty : theme.Author.Trim(),
                    IsBuiltIn = false,
                    Colors = colors
                };
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static void RebuildBrushes(ResourceDictionary resources)
    {
        foreach(var key in BrushKeys)
        {
            if (resources.TryGetValue(key, out var value) && value is Color color)
            {
                resources[$"{key}Brush"] = new SolidColorBrush(color);
            }
        }
    }

    private static Boolean TryParseColor(String value, out Color color)
    {
        try
        {

            color = Color.FromArgb(value);
            return true;
        }
        catch
        {
            color = Colors.Transparent;
            return false;
        }
    }

    private AppTheme GetAppTheme() => _settings.AppTheme switch
    {
        AppThemeSetting.Dark => Microsoft.Maui.ApplicationModel.AppTheme.Dark,
        AppThemeSetting.Light => Microsoft.Maui.ApplicationModel.AppTheme.Light,
        _ => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified,
    };
    #endregion    
}

