using TheRandomizer.Application.Enumerators;
using TheRandomizer.Application.Interfaces;

namespace TheRandomizer.Maui.Resources.Templates;

internal partial class Output(IAppSettingsService settings, 
                              IThemeService themeService, 
                              GeneratorResult content)
{
    private readonly IAppSettingsService _settings = settings;
    private readonly IThemeService _themeService = themeService;

    private AppTheme Theme { get => _settings.AppTheme switch
                                    {
                                        AppThemeSetting.Dark => AppTheme.Dark,
                                        AppThemeSetting.Light => AppTheme.Light,
                                        _ => Microsoft.Maui.Controls.Application.Current?.RequestedTheme ?? AppTheme.Light
                                    }; 
                            }
    private GeneratorResult Content { get; } = content;
    private String Style { get => @$"body {{
                    color: {(Theme == AppTheme.Dark ? "#dddddd" : "#000000")};
                }}"; }
    private String CustomCSS
    {
        get
        {
            if (!String.IsNullOrWhiteSpace(_themeService.CustomCSSFile))
                return File.ReadAllText(_themeService.CustomCSSFile);
            return String.Empty;
        }
    }
}

