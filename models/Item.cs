namespace tuipos.Models;

public class Item
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Barcode { get; set; } = "";

    // $5.99 is stored as 599.
    public long PriceCents { get; set; }
}
