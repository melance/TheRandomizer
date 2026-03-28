using TheRandomizer.Application.Interfaces;
using TheRandomizer.Application.Models;
using TheRandomizer.Application.Utility;

namespace TheRandomizer.Application.Services;

public class GeneratorFileBrowser : IGeneratorFileBrowser
{    
    public async Task<IReadOnlyList<GeneratorFileItem>> GetFilesAsync(String folderPath)
    {
        var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                             .Where(ValidExtension)
                             .Select(f => new GeneratorFileItem(f))
                             .Where(gfi => gfi.Summary.Show)
                             .OrderBy(gfi => gfi.Name)
                             .ToList();
        return files;
    }

    public Task<IReadOnlyList<String>> GetSubfoldersAsync(String root)
    {
        if (!Directory.Exists(root))
            return Task.FromResult<IReadOnlyList<String>>([]);

        IReadOnlyList<String> folders = [.. Directory
                                            .EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                                            .Select(path => Path.GetRelativePath(root, path))
                                            .OrderBy(x => x)];

        return Task.FromResult(folders);
    }

    private static Boolean ValidExtension(String fileName)
    {
        foreach (var extension in Constants.GeneratorExtensions)
        {
            if (fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

