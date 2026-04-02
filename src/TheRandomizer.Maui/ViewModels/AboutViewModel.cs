using CommunityToolkit.Mvvm.Input;
using TheRandomizer.Application.Interfaces;
using TheRandomizer.Maui.Utilities;

namespace TheRandomizer.Maui.ViewModels;

public partial class AboutViewModel(IGeneratorFolderService folderService,
                                    IThemeService themeService)
{
    private readonly IGeneratorFolderService _folderService = folderService;
    private readonly IThemeService _themeService = themeService;

    public String Name => "Test Name";
    public String Version => $"v {AppInfo.Version.ToString(2)}";
    public String Build => $"{AppInfo.Version} - {AppInfo.BuildString}";
    public String PackageName => AppInfo.PackageName;
    public String Description => AppMetadata.Description;
    public String Author => AppMetadata.Author;
    public String GeneratorDirectory => _folderService.GetCurrentFolderAsync().Result;
    public String ThemeDirectory => _themeService.ThemesFolder;
    public String CustomCSSFile => _themeService.CustomCSSFile;
    public String GitHubUrl => AppMetadata.GitHubUrl;
    public String Subreddit => AppMetadata.Subreddit;

    [RelayCommand]
    private void CopyPath(String path)
    {
        Clipboard.Default.SetTextAsync(path);
    }
}

