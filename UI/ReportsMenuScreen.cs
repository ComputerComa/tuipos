using Terminal.Gui.App;
using tuipos.Data;
using tuipos.Models;
using tuipos.Reports;

namespace tuipos.UI;

// Main menu option 3. Each report is saved as a spooled file, then shown,
// the way you'd check printer output before sending it to the printer.
public class ReportsMenuScreen
{
    private readonly PosDbContext _db;
    private readonly PosUser _user;

    // The spool folder sits next to pos.db, in the working directory.
    private readonly SpoolStore _spool = new("spool");

    public ReportsMenuScreen(PosDbContext db, PosUser user)
    {
        _db = db;
        _user = user;
    }

    public MenuResult Run(IApplication app)
    {
        var menu = new MenuScreen("REPORTS", "TUIPOS Reports", _user, isInitialMenu: false);

        menu.AddOption(1, "Daily sales summary", s => RunDated(
            s,
            DailySalesSummaryReport.Id,
            DailySalesSummaryReport.Title,
            dateRange: false,
            (from, _) => DailySalesSummaryReport.Build(_db, from, _user.UserId)));

        menu.AddOption(2, "Sales journal", s => RunDated(
            s,
            SalesJournalReport.Id,
            SalesJournalReport.Title,
            dateRange: true,
            (from, to) => SalesJournalReport.Build(_db, from, to, _user.UserId)));

        menu.AddOption(3, "Sales by item", s => RunDated(
            s,
            SalesByItemReport.Id,
            SalesByItemReport.Title,
            dateRange: true,
            (from, to) => SalesByItemReport.Build(_db, from, to, _user.UserId)));

        menu.AddOption(4, "Item price list", s => Spool(s, PriceListReport.Build(_db, _user.UserId)));

        menu.AddOption(5, "Work with spooled files", s => new WorkWithSpooledFilesScreen(_spool).Run(s.App));

        return menu.Run(app);
    }

    private void RunDated(
        MenuSelection selection,
        string reportId,
        string title,
        bool dateRange,
        Func<DateOnly, DateOnly, Report> build)
    {
        var dates = new ReportPromptScreen(reportId, title, dateRange).Run(selection.App);

        if (dates is { } chosen)
        {
            Spool(selection, build(chosen.From, chosen.To));
        }
    }

    private void Spool(MenuSelection selection, Report report)
    {
        var file = _spool.Save(report, _user.UserId);

        new DisplaySpooledFileScreen(file).Run(selection.App);

        selection.Message.Show(Messages.SpooledFileCreated(file.Number, file.Name, file.Pages));
    }
}
