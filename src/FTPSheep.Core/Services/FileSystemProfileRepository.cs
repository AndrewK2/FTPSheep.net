using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Models;

namespace FTPSheep.Core.Services;

internal class FileSystemProfileRepository : IProfileRepository {
    public Task SaveAsync(string filePath, DeploymentProfile profile, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    public Task<DeploymentProfile?> LoadAsync(string filePath, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    public Task<DeploymentProfile> LoadFromPathAsync(string filePath, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    public Task<List<string>> ListProfileNamesAsync(CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }
}
