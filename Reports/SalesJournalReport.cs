using tuipos.Data;
using tuipos.UI;

namespace tuipos.Reports;

// Every sale in a date range, with its lines, like a printed journal tape.
public static class SalesJournalReport
{
    public const string Id = "SLSJRN";
    public const string Title = "Sales Journal";

    public static Report Build(PosDbContext db, DateOnly from, DateOnly to, string userId)
    {
        var sales = SalesData.Between(db, from, to);
        var report = new ReportWriter(Id, Title, userId);

        report.Subheading($"Date range . . . . . . . :   {SalesData.RangeText(from, to)}");
        report.ColumnHeadings(
            Detail("Sale", "Date", "Time", "Cashier", "Payment", "Barcode", "Description", "Qty", "Price", "Amount"),
            Detail(
                new string('-', 7), new string('-', 8), new string('-', 8), new string('-', 10),
                new string('-', 7), new string('-', 13), new string('-', 30), new string('-', 5),
                new string('-', 9), new string('-', 10))
        );

        foreach (var sale in sales)
        {
            var created = sale.CreatedAt.LocalDateTime;
            bool firstLine = true;

            foreach (var item in sale.Items.OrderBy(i => i.Id))
            {
                // The sale's own columns only print on its first line.
                report.Line(Detail(
                    firstLine ? sale.Id.ToString() : "",
                    firstLine ? SalesData.DateText(created) : "",
                    firstLine ? SalesData.TimeText(created) : "",
                    firstLine ? sale.User.UserId : "",
                    firstLine ? sale.PaymentMethod.ToString() : "",
                    item.Barcode,
                    GreenScreen.Clip(item.ItemName, 30),
                    item.Quantity.ToString(),
                    GreenScreen.Amount(item.UnitPriceCents),
                    GreenScreen.Amount(item.LineTotalCents)));

                firstLine = false;
            }

            report.Line(Detail("", "", "", "", "", "", "Sale total", "", "", GreenScreen.Amount(sale.TotalCents)));
            report.Line();
        }

        if (sales.Count == 0)
        {
            report.Line("(No sales in this date range)");
            report.Line();
        }

        report.Line($"Sales  . . . . . . . . . :   {sales.Count}");
        report.Line($"Items  . . . . . . . . . :   {sales.Sum(s => s.Items.Sum(i => i.Quantity))}");
        report.Line($"Total  . . . . . . . . . :   {GreenScreen.Amount(sales.Sum(s => s.TotalCents))}");

        return report.Finish();
    }

    private static string Detail(
        string sale,
        string date,
        string time,
        string cashier,
        string payment,
        string barcode,
        string description,
        string quantity,
        string price,
        string amount)
    {
        return $"{sale,7}  {date,-8}  {time,-8}  {cashier,-10}  {payment,-7}  {barcode,-13}  "
            + $"{description,-30}  {quantity,5}  {price,9}  {amount,10}";
    }
}
