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
            entity.Property(e => e.PeerCount).HasColumnName("peer_count");
            entity.Property(e => e.UnconfirmedCount).HasColumnName("unconfirmed_count");
            entity.Property(e => e.HighFeePerKb).HasColumnName("high_fee_per_kb");
            entity.Property(e => e.MediumFeePerKb).HasColumnName("medium_fee_per_kb");
            entity.Property(e => e.LowFeePerKb).HasColumnName("low_fee_per_kb");
            entity.Property(e => e.HighGasPrice).HasColumnName("high_gas_price");
            entity.Property(e => e.MediumGasPrice).HasColumnName("medium_gas_price");
            entity.Property(e => e.LowGasPrice).HasColumnName("low_gas_price");
            entity.Property(e => e.LastForkHeight).HasColumnName("last_fork_height");
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
