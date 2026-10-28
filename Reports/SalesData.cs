using System.Globalization;
using Microsoft.EntityFrameworkCore;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.Reports;

public static class SalesData
{
    // Sales rung up between two local dates, inclusive, oldest first, with
    // their lines and cashier. SQLite can't compare DateTimeOffset values in
    // a query, so the date filter runs in memory.
    public static List<Sale> Between(PosDbContext db, DateOnly from, DateOnly to)
    {
        return db.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.User)
            .AsEnumerable()
            .Where(s =>
            {
                var date = DateOnly.FromDateTime(s.CreatedAt.LocalDateTime);
                return date >= from && date <= to;
            })
            .OrderBy(s => s.CreatedAt)
            .ToList();
    }

    public static string DateText(DateOnly date)
    {
        return date.ToString("MM/dd/yy", CultureInfo.InvariantCulture);
    }

    public static string DateText(DateTime value)
    {
        return DateText(DateOnly.FromDateTime(value));
    }

    public static string TimeText(DateTime value)
    {
        return value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public static string RangeText(DateOnly from, DateOnly to)
    {
        return from == to ? DateText(from) : $"{DateText(from)} - {DateText(to)}";
    }
}
