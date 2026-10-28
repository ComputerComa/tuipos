using Terminal.Gui.App;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

public class MainMenuScreen
{
    private readonly PosDbContext _db;
    private readonly PosUser _user;

    public MainMenuScreen(PosDbContext db, PosUser user)
    {
        _db = db;
        _user = user;
    }

    // Returns when the user signs off.
    public void Run(IApplication app)
    {
        var signedOnAt = DateTime.Now;

        var menu = new MenuScreen("MAIN", "TUIPOS Main Menu", _user, isInitialMenu: true);

        menu.AddOption(1, "Register", s => new RegisterScreen(_db, _user).Run(s.App));
        menu.AddOption(2, "Item inquiry", NotAvailable);
        menu.AddOption(3, "Reports", OpenReports);
        menu.AddOption(4, "Display user information", s => new UserInfoScreen(_user, signedOnAt).Run(s.App));
        menu.AddOption(5, "Administration", OpenAdministration);

        menu.Run(app);
    }

    private void OpenReports(MenuSelection selection)
    {
        var result = new ReportsMenuScreen(_db, _user).Run(selection.App);
        selection.SignOff = result == MenuResult.SignOff;
    }

    private void OpenAdministration(MenuSelection selection)
    {
        if (!_user.IsAdmin)
        {
            selection.Message.Error(Messages.NotAuthorized("Administration"));
            return;
        }

        var result = new AdminMenuScreen(_db, _user).Run(selection.App);
        selection.SignOff = result == MenuResult.SignOff;
    }

    private static void NotAvailable(MenuSelection selection)
    {
        selection.Message.Show(Messages.OptionNotAvailable(selection.Option));
    }
}
