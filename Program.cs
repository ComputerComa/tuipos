using Microsoft.EntityFrameworkCore;
using tuipos;

using var db = new PosDbContext();

await db.Database.MigrateAsync();

const string barcode = "012345678901";

var item = await db.Items
    .SingleOrDefaultAsync(i => i.Barcode == barcode);

if (item is null)
{
    item = new Item
    {
        Barcode = barcode,
        Name = "Coffee",
        PriceCents = 599
    };

    db.Items.Add(item);
    await db.SaveChangesAsync();

    Console.WriteLine("Created test item.");
}

Console.WriteLine(
    $"{item.Name}: ${item.PriceCents / 100m:F2} " +
    $"(barcode: {item.Barcode})");
