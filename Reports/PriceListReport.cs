using Microsoft.EntityFrameworkCore;
using tuipos.Data;
using tuipos.UI;

namespace tuipos.Reports;

// The whole catalog in description order, to keep by the register.
public static class PriceListReport
{
    public const string Id = "PRCLST";
    public const string Title = "Item Price List";

    public static Report Build(PosDbContext db, string userId)
    {
        var items = db.Items.AsNoTracking().OrderBy(i => i.Name).ToList();
        var report = new ReportWriter(Id, Title, userId);

        report.ColumnHeadings(
            Row("Barcode", "Description", "Price"),
            Row(new string('-', 13), new string('-', 30), new string('-', 9))
        );

        foreach (var item in items)
        {
            report.Line(Row(item.Barcode, GreenScreen.Clip(item.Name, 30), GreenScreen.Amount(item.PriceCents)));
        }

        if (items.Count == 0)
        {
            report.Line("(No items)");
        }

        report.Line();
        report.Line($"Items  . . . . . . . . . :   {items.Count}");

        return report.Finish();
    }

    private static string Row(string barcode, string description, string price)
    {
        return $"{barcode,-13}  {description,-30}  {price,9}";
    }
}
