using UnifiedStorage.Domain.Entities;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Application.Common.Interfaces;

public interface IStorageConnectionRepository
{
    Task<StorageConnection?> GetByUserAndProviderAsync(string userId, StorageProvider provider, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageConnection>> GetAllByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(StorageConnection connection, CancellationToken cancellationToken = default);
    Task UpdateAsync(StorageConnection connection, CancellationToken cancellationToken = default);
    Task DeleteAsync(StorageConnection connection, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string userId, StorageProvider provider, CancellationToken cancellationToken = default);
}
