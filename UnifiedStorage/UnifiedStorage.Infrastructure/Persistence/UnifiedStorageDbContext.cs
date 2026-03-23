using Microsoft.EntityFrameworkCore;
using UnifiedStorage.Domain.Entities;

namespace UnifiedStorage.Infrastructure.Persistence;

public class UnifiedStorageDbContext : DbContext
{
    public UnifiedStorageDbContext(DbContextOptions<UnifiedStorageDbContext> options)
        : base(options) { }

    public DbSet<StorageConnection> StorageConnections => Set<StorageConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UnifiedStorageDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
