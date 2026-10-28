using System.Globalization;
using tuipos.Data;
using tuipos.UI;

namespace tuipos.Reports;

// Units and revenue per item over a date range, best sellers first.
public static class SalesByItemReport
{
    public const string Id = "SLSITM";
    public const string Title = "Sales by Item";

    public static Report Build(PosDbContext db, DateOnly from, DateOnly to, string userId)
    {
        var sales = SalesData.Between(db, from, to);
        var report = new ReportWriter(Id, Title, userId);

        report.Subheading($"Date range . . . . . . . :   {SalesData.RangeText(from, to)}");
        report.ColumnHeadings(
            Row("Barcode", "Description", "Units", "Revenue", "Percent"),
            Row(new string('-', 13), new string('-', 30), new string('-', 8), new string('-', 12), new string('-', 8))
        );

        // Group on the barcode saved with each sale line, so items deleted
        // since still show up. The newest sale supplies the description.
        var rows = sales
            .SelectMany(s => s.Items)
            .GroupBy(i => i.Barcode)
            .Select(g => new
            {
                Barcode = g.Key,
                Description = g.Last().ItemName,
                Units = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.LineTotalCents)
            })
            .OrderByDescending(r => r.Revenue)
            .ThenBy(r => r.Barcode)
            .ToList();

        long total = rows.Sum(r => r.Revenue);

        foreach (var row in rows)
        {
            report.Line(Row(
                row.Barcode,
                GreenScreen.Clip(row.Description, 30),
                row.Units.ToString(),
                GreenScreen.Amount(row.Revenue),
                Percent(row.Revenue, total)));
        }

        if (rows.Count == 0)
        {
            report.Line("(No sales in this date range)");
        }

        report.Line(Row("", "", new string('-', 8), new string('-', 12), new string('-', 8)));
        report.Line(Row(
            "",
            "Total",
            rows.Sum(r => r.Units).ToString(),
            GreenScreen.Amount(total),
            total == 0 ? "" : "100.0"));

        return report.Finish();
    }

    private static string Percent(long part, long total)
    {
        return total == 0
            ? ""
            : (part * 100.0 / total).ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static string Row(string barcode, string description, string units, string revenue, string percent)
    {
        return $"{barcode,-13}  {description,-30}  {units,8}  {revenue,12}  {percent,8}";
    }
}
