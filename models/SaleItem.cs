namespace tuipos.Models;

public class SaleItem
{
    public int Id { get; set; }

    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    // Optional link to the current catalog item.
    public int? ItemId { get; set; }
    public Item? Item { get; set; }

    // Snapshots taken when the item is sold.
    public string ItemName { get; set; } = "";
    public string Barcode { get; set; } = "";
    public long UnitPriceCents { get; set; }

    public int Quantity { get; set; }

    // Snapshot of unit price × quantity.
    public long LineTotalCents { get; set; }
}
