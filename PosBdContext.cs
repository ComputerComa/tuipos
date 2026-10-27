using Microsoft.EntityFrameworkCore;

namespace tuipos;

public class Item
{
    public int Id { get; set; }

    public string Barcode { get; set; } = "";
    public string Name { get; set; } = "";

    // Store currency as whole cents.
    public long PriceCents { get; set; }
}

public class PosDbContext : DbContext
{
    public DbSet<Item> Items => Set<Item>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=pos.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>()
            .HasIndex(item => item.Barcode)
            .IsUnique();

        modelBuilder.Entity<Item>()
            .Property(item => item.Barcode)
            .IsRequired();

        modelBuilder.Entity<Item>()
            .Property(item => item.Name)
            .IsRequired();
    }
}
