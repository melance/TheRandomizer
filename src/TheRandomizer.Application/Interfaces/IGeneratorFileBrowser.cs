using TheRandomizer.Application.Models;

namespace TheRandomizer.Application.Interfaces;

public interface IGeneratorFileBrowser
{
    Task<IReadOnlyList<GeneratorFileItem>> GetFilesAsync(String folderPath);
    Task<IReadOnlyList<String>> GetSubfoldersAsync(String root);
}
