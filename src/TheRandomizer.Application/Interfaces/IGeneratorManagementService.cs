using TheRandomizer.Application.Models.Results;

namespace TheRandomizer.Application.Interfaces;

public interface IGeneratorManagementService
{
    Task<ImportGeneratorResult> ImportAsync(Stream stream,
                                            String fileName,
                                            String? subFolder = null,
                                            CancellationToken cancellationToken = default);
    void Delete(String fileName);
    Task SaveAsync(BaseGenerator generator);
}
