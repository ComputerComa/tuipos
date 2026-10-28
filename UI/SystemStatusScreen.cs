using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Data;

namespace tuipos.UI;

// Administration option 4: counts and sizes at a glance. F5 refreshes.
public class SystemStatusScreen
{
    private const int PromptWidth = 31;
    private const int ValueX = 37;

    private static readonly string[] Prompts =
    {
        "Users",
        "Administrators",
        "Items",
        "Sales",
        "Sales today",
        "Sales total today",
        "Database file",
        "Database size",
        "Up time"
    };

    private readonly PosDbContext _db;

    public SystemStatusScreen(PosDbContext db)
    {
        _db = db;
    }

    public void Run(IApplication app)
    {
        using GreenScreenWindow window = new("F3=Exit   F5=Refresh   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "DSPSYSSTS", "Display System Status");

        var values = new Label[Prompts.Length];

        for (int i = 0; i < Prompts.Length; i++)
        {
            values[i] = GreenScreen.Text(ValueX, 4 + i, "", GreenScreen.Bright);
            values[i].Width = GreenScreen.Columns - ValueX - 1;

            window.Grid.Add(
                GreenScreen.Text(1, 4 + i, GreenScreen.DotLeader(Prompts[i], PromptWidth) + " :"),
                values[i]
            );
        }

        void Refresh()
        {
            string[] current = CurrentValues();

            for (int i = 0; i < values.Length; i++)
            {
                values[i].Text = current[i];
            }
        }

        window.KeyDown += (_, key) =>
        {
            if (key == Key.F5)
            {
                key.Handled = true;
                Refresh();
            }
            else if (key == Key.Enter || key == Key.F3 || key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                app.RequestStop();
            }
        };

        window.Grid.Add(GreenScreen.Text(1, 20, "Press Enter to continue."));

        Refresh();
        window.RunWithClock(app);
    }

    // In the same order as Prompts.
    private string[] CurrentValues()
    {
        // SQLite can't compare DateTimeOffset values in a query, so the
        // "today" filter runs here instead of in the database.
        var today = DateTime.Today;
        var todaysSales = _db.Sales
            .AsNoTracking()
            .Select(s => new { s.CreatedAt, s.TotalCents })
            .AsEnumerable()
            .Where(s => s.CreatedAt.LocalDateTime.Date == today)
            .ToList();

        string databaseFile = _db.Database.GetDbConnection().DataSource;
        var file = new FileInfo(databaseFile);

        // Keep the end of a long path, where the file name is.
        int room = GreenScreen.Columns - ValueX - 1;
        string path = file.FullName.Length > room
            ? "..." + file.FullName[^(room - 3)..]
            : file.FullName;

        var upTime = DateTime.Now - Process.GetCurrentProcess().StartTime;

        return new[]
        {
            _db.Users.Count().ToString(),
            _db.Users.Count(u => u.IsAdmin).ToString(),
            _db.Items.Count().ToString(),
            _db.Sales.Count().ToString(),
            todaysSales.Count.ToString(),
            GreenScreen.Amount(todaysSales.Sum(s => s.TotalCents)),
            path,
            file.Exists ? file.Length.ToString("N0", CultureInfo.InvariantCulture) + " bytes" : "Not found",
            $"{(int)upTime.TotalHours:00}:{upTime.Minutes:00}:{upTime.Seconds:00}"
        };
    }
}
