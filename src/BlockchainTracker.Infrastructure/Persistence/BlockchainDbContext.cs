using BlockchainTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlockchainTracker.Infrastructure.Persistence;

public class BlockchainDbContext : DbContext
{
    public BlockchainDbContext(DbContextOptions<BlockchainDbContext> options) : base(options) { }

    public DbSet<BlockchainSnapshot> Snapshots => Set<BlockchainSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlockchainSnapshot>(entity =>
        {
            entity.ToTable("blockchain_snapshots");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChainName).HasColumnName("chain_name").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.Hash).HasColumnName("hash").HasMaxLength(128).IsRequired();
            entity.Property(e => e.Time).HasColumnName("time");
            entity.Property(e => e.RawJson).HasColumnName("raw_json").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.FetchedAt).HasColumnName("fetched_at");

            entity.HasIndex(e => new { e.ChainName, e.Height, e.Hash })
                .IsUnique()
                .HasDatabaseName("ix_snapshots_chain_height_hash");

            entity.HasIndex(e => new { e.ChainName, e.FetchedAt })
                .HasDatabaseName("ix_snapshots_chain_fetched_at");
        });
    }
}
