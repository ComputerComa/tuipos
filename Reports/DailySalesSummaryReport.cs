using tuipos.Data;
using tuipos.Models;
using tuipos.UI;

namespace tuipos.Reports;

// One day's sales, totaled by payment method and by cashier.
public static class DailySalesSummaryReport
{
    public const string Id = "DLYSLS";
    public const string Title = "Daily Sales Summary";

    public static Report Build(PosDbContext db, DateOnly date, string userId)
    {
        var sales = SalesData.Between(db, date, date);
        var report = new ReportWriter(Id, Title, userId);

        report.Subheading($"Report date  . . . . . . :   {SalesData.DateText(date)}");

        report.Line(Row("Payment method", "Sales", "Items", "Amount"));
        report.Line(Rule());

        foreach (var method in Enum.GetValues<PaymentMethod>())
        {
            report.Line(Totals(method.ToString(), sales.Where(s => s.PaymentMethod == method)));
        }

        report.Line(Rule());
        report.Line(Totals("Total", sales));
        report.Line();

        report.Line(Row("Cashier", "Sales", "Items", "Amount"));
        report.Line(Rule());

        foreach (var cashier in sales.GroupBy(s => s.UserId).OrderBy(g => g.First().User.UserId))
        {
            var user = cashier.First().User;
            report.Line(Totals($"{user.UserId}  {user.FirstName} {user.LastName}", cashier));
        }

        if (sales.Count == 0)
        {
            report.Line("(No sales on this date)");
        }

        if (sales.Count > 0)
        {
            long total = sales.Sum(s => s.TotalCents);
            long average = (total + sales.Count / 2) / sales.Count;

            report.Line();
            report.Line($"Average sale . . . . . . :   {GreenScreen.Amount(average)}");
            report.Line($"First sale . . . . . . . :   {SalesData.TimeText(sales[0].CreatedAt.LocalDateTime)}");
            report.Line($"Last sale  . . . . . . . :   {SalesData.TimeText(sales[^1].CreatedAt.LocalDateTime)}");
        }

        return report.Finish();
    }

    private static string Totals(string label, IEnumerable<Sale> sales)
    {
        var list = sales.ToList();

        return Row(
            GreenScreen.Clip(label, 30),
            list.Count.ToString(),
            list.Sum(s => s.Items.Sum(i => i.Quantity)).ToString(),
            GreenScreen.Amount(list.Sum(s => s.TotalCents))
        );
    }

    private static string Row(string label, string sales, string items, string amount)
    {
        return $"{label,-30}{sales,8}{items,8}{amount,14}";
    }

    private static string Rule()
    {
        return Row(new string('-', 30), new string('-', 6), new string('-', 6), new string('-', 12));
    }
}
