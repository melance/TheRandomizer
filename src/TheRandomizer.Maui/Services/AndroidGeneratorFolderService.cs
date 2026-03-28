using TheRandomizer.Application.Interfaces;

namespace TheRandomizer.Maui.Services;

public class AndroidGeneratorFolderService() : IGeneratorFolderService
{
    public Boolean CanBrowse => false;

    public Task<String?> BrowseAsync(CancellationToken cancellation = default) => Task.FromResult<String?>(null);    

    public Task<String> GetCurrentFolderAsync(CancellationToken cancellation = default) => Task.FromResult(Path.Combine(FileSystem.AppDataDirectory, "Definitions"));
}

