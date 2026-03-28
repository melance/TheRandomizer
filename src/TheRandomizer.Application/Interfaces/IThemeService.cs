using TheRandomizer.Application.Models;
using TheRandomizer.Application.Models.Results;

namespace TheRandomizer.Application.Interfaces;

public interface IThemeService
{
    String ThemesFolder { get; }

    void ReapplyTheme();
    void ApplyTheme(String themeId);
    Task<ThemeDefinition?> GetThemeAsync(string themeId);
    Task<IReadOnlyList<ThemeDefinition>> GetThemesAsync();
    Task<ImportThemeResult> ImportThemeAsync(String sourceFilePath, Boolean overwrite = false);
    Task DeleteThemeAsync(String themeId);
}
