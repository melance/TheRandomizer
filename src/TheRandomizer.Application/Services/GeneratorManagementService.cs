using TheRandomizer.Application.Interfaces;
using TheRandomizer.Application.Models.Results;
using TheRandomizer.Application.Utility;

namespace TheRandomizer.Application.Services;

public class GeneratorManagementService(IAppSettingsService settings) : BaseService, IGeneratorManagementService
{
    private readonly IAppSettingsService _settings = settings;

    public async Task<ImportGeneratorResult> ImportAsync(Stream stream, 
                                                         String fileName, 
                                                         String? subfolder = null,
                                                         CancellationToken cancellationToken = default)
    {
        try
        {
            var generatorFolder = _settings.GeneratorFolder;
            if (String.IsNullOrWhiteSpace(generatorFolder))
                return ErrorResult<ImportGeneratorResult>("No generator folder is configured.");

            var extension = Path.GetExtension(fileName);

            if (!Constants.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return ErrorResult< ImportGeneratorResult>($"Unsupported file type {extension}.");

            var safeSubFolder = NormalizeSubfolder(subfolder);

            var destinationFolder = String.IsNullOrWhiteSpace(safeSubFolder)
                ? generatorFolder
                : Path.Combine(generatorFolder, safeSubFolder);

            Directory.CreateDirectory(destinationFolder);

            var destinationPath = Path.Combine(destinationFolder, fileName);

            await using (var output = File.Create(destinationPath))
            {
                await stream.CopyToAsync(output, cancellationToken);
            }

            return new()
            {
                Success = true,
                ImportedPath = destinationPath,
                StoredFilename = fileName
            };
            }
        catch (Exception ex)
        {
            return ExceptionResult<ImportGeneratorResult>(ex);
        }
    }

    public void Delete(String filePath)
    {
        File.Delete(filePath);
    }

    public Task SaveAsync(BaseGenerator generator)
    {
        var json = BaseGenerator.Serialize(generator);
        return File.WriteAllTextAsync(generator.FilePath, json);
    }

    private static string NormalizeSubfolder(string? subfolder)
    {
        if (string.IsNullOrWhiteSpace(subfolder))
            return string.Empty;

        var parts = subfolder
            .Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x != "." && x != "..")
            .ToArray();

        return Path.Combine(parts);
    }
}

