using System.Globalization;
using Microsoft.EntityFrameworkCore;
using tuipos.Models;

namespace tuipos.Data;

// Barcodes the store assigns itself, like the seeded 100000000001. The long
// prefix keeps them clear of real UPC barcodes that start with 1. The BARCODE
// data area holds the highest one ever saved, so a barcode isn't handed out
// again after its item is deleted.
public static class StoreBarcodes
{
    public const string DataAreaName = "BARCODE";

    private const string Prefix = "1000000";
    private const int Length = 12;
    private const long First = 100000000001;

    public static bool IsStoreBarcode(string barcode)
    {
        return barcode.Length == Length
            && barcode.StartsWith(Prefix, StringComparison.Ordinal)
            && barcode.All(char.IsAsciiDigit);
    }

    // The barcode F6 fills in on Create Item.
    public static string Next(PosDbContext db)
    {
        long saved = db.DataAreas
            .AsNoTracking()
            .Where(a => a.Name == DataAreaName)
            .Select(a => (long?)a.Value)
            .FirstOrDefault() ?? 0;

        // The barcodes in use are checked too, in case an item got into the
        // database some other way.
        return (Math.Max(saved, HighestInUse(db)) + 1).ToString(CultureInfo.InvariantCulture);
    }

    // Raises the BARCODE data area to a new item's barcode. Call it before
    // saving the item, so both are saved together.
    public static void Record(PosDbContext db, string barcode)
    {
        if (!IsStoreBarcode(barcode))
        {
            return;
        }

        long value = long.Parse(barcode, CultureInfo.InvariantCulture);
        var area = db.DataAreas.FirstOrDefault(a => a.Name == DataAreaName);

        if (area is null)
        {
            db.DataAreas.Add(new DataArea { Name = DataAreaName, Value = value });
        }
        else if (value > area.Value)
        {
            area.Value = value;
        }
    }

    // Creates the BARCODE data area, starting from the barcodes already in
    // use. It's never recalculated afterwards, so deleted items still count.
    public static void EnsureDataArea(PosDbContext db)
    {
        if (db.DataAreas.Any(a => a.Name == DataAreaName))
        {
            return;
        }

        db.DataAreas.Add(new DataArea { Name = DataAreaName, Value = HighestInUse(db) });
    }

    // The highest store barcode on an item or in sales history.
    private static long HighestInUse(PosDbContext db)
    {
        var itemBarcodes = db.Items
            .AsNoTracking()
            .Where(i => i.Barcode.StartsWith(Prefix))
            .Select(i => i.Barcode)
            .ToList();

        var soldBarcodes = db.SaleItems
            .AsNoTracking()
            .Where(s => s.Barcode.StartsWith(Prefix))
            .Select(s => s.Barcode)
            .Distinct()
            .ToList();

        return itemBarcodes
            .Concat(soldBarcodes)
            .Where(IsStoreBarcode)
            .Select(b => long.Parse(b, CultureInfo.InvariantCulture))
            .DefaultIfEmpty(First - 1)
            .Max();
    }
}
