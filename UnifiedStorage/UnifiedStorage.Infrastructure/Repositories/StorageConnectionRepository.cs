using Microsoft.EntityFrameworkCore;
using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Domain.Entities;
using UnifiedStorage.Domain.Enums;
using UnifiedStorage.Infrastructure.Persistence;

namespace UnifiedStorage.Infrastructure.Repositories;

public class StorageConnectionRepository : IStorageConnectionRepository
{
    private readonly UnifiedStorageDbContext _context;

    public StorageConnectionRepository(UnifiedStorageDbContext context)
    {
        _context = context;
    }

    public async Task<StorageConnection?> GetByUserAndProviderAsync(
        string userId, StorageProvider provider, CancellationToken cancellationToken = default)
    {
        return await _context.StorageConnections
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Provider == provider, cancellationToken);
    }

    public async Task<IReadOnlyList<StorageConnection>> GetAllByUserAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        return await _context.StorageConnections
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StorageConnection connection, CancellationToken cancellationToken = default)
    {
        await _context.StorageConnections.AddAsync(connection, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StorageConnection connection, CancellationToken cancellationToken = default)
    {
        _context.StorageConnections.Update(connection);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(StorageConnection connection, CancellationToken cancellationToken = default)
    {
        _context.StorageConnections.Remove(connection);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string userId, StorageProvider provider, CancellationToken cancellationToken = default)
    {
        return await _context.StorageConnections
            .AnyAsync(c => c.UserId == userId && c.Provider == provider, cancellationToken);
    }
}
