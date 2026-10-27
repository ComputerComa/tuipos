namespace tuipos.Models;

public enum PaymentMethod
{
    Cash,
    Card,
    Other
}

public class Sale
{
    public int Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Snapshot of the completed sale total.
    public long TotalCents { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    // The user who created the sale.
    public int UserId { get; set; }
    public PosUser User { get; set; } = null!;

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
