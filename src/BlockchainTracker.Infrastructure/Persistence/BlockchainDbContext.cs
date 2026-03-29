using BlockchainTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlockchainTracker.Infrastructure.Persistence;

public sealed class BlockchainDbContext(DbContextOptions<BlockchainDbContext> options) : DbContext(options)
{
    public DbSet<BlockchainSnapshot> Snapshots => Set<BlockchainSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BlockchainDbContext).Assembly);
    }
}
