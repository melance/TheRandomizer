using TheRandomizer.Application.Models.Results;

namespace TheRandomizer.Application.Interfaces;

internal interface IImportGeneratorDialogService
{
    Task<ImportGeneratorDialogResult> ShowAsync(String filename);
}
