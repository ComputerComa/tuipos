using Microsoft.EntityFrameworkCore;
using tuipos.Models;

namespace tuipos.Data;

public class PosDbContext : DbContext
{
    public DbSet<Item> Items => Set<Item>();
    public DbSet<PosUser> Users => Set<PosUser>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<DataArea> DataAreas => Set<DataArea>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=pos.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasIndex(item => item.Barcode).IsUnique();

            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.Barcode).IsRequired();
        });

        modelBuilder.Entity<PosUser>(entity =>
        {
            entity.HasIndex(user => user.UserId).IsUnique();

            entity.Property(user => user.UserId).IsRequired();
            entity.Property(user => user.FirstName).IsRequired();
            entity.Property(user => user.LastName).IsRequired();
            entity.Property(user => user.PinHash).IsRequired();
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasOne(sale => sale.User)
                .WithMany(user => user.Sales)
                .HasForeignKey(sale => sale.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(sale => sale.Items)
                .WithOne(item => item.Sale)
                .HasForeignKey(item => item.SaleId);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.Property(item => item.ItemName).IsRequired();
            entity.Property(item => item.Barcode).IsRequired();

            entity.HasOne(item => item.Item)
                .WithMany()
                .HasForeignKey(item => item.ItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DataArea>(entity =>
        {
            entity.HasKey(area => area.Name);
        });
    }
}
