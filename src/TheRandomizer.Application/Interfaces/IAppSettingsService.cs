using TheRandomizer.Application.Enumerators;

namespace TheRandomizer.Application.Interfaces;

public interface IAppSettingsService
{
    String GeneratorFolder { get; set; }
    String SelectedTheme { get; set; }
    String FontFamily { get; set; }
    FontSizes FontSize { get; set; }
    AppThemeSetting AppTheme { get; set; }
}
