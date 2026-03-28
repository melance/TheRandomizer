namespace TheRandomizer.Application.Interfaces;

public interface IGeneratorFolderService
{
    Task<String> GetCurrentFolderAsync(CancellationToken cancellation = default);
    Boolean CanBrowse { get; }
    Task<String?> BrowseAsync(CancellationToken cancellation = default);
}
