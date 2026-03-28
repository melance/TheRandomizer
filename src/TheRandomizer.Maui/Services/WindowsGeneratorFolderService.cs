using CommunityToolkit.Maui.Storage;
using TheRandomizer.Application.Interfaces;

namespace TheRandomizer.Maui.Services;

public class WindowsGeneratorFolderService(IAppSettingsService settings,
                                           IFolderPicker filePicker) : IGeneratorFolderService
{
    private readonly IAppSettingsService _settings = settings;
    private readonly IFolderPicker _folderPicker = filePicker;

    public Boolean CanBrowse => true;

    public async Task<String?> BrowseAsync(CancellationToken cancellation = default)
    {
        var result = await _folderPicker.PickAsync(CancellationToken.None);
        if (!result.IsSuccessful || result.Folder is null) return null;
        _settings.GeneratorFolder = result.Folder.Path;
        return result.Folder.Path;
    }

    public Task<String> GetCurrentFolderAsync(CancellationToken cancellation = default) =>
        Task.FromResult(String.IsNullOrWhiteSpace(_settings.GeneratorFolder) 
                        ? Path.Combine(FileSystem.AppDataDirectory, "Definitions")
                        : _settings.GeneratorFolder);
}

