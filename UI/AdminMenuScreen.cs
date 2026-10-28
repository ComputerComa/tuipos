using Terminal.Gui.App;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

// Main menu option 5, for administrators only.
public class AdminMenuScreen
{
    private readonly PosDbContext _db;
    private readonly PosUser _user;

    public AdminMenuScreen(PosDbContext db, PosUser user)
    {
        _db = db;
        _user = user;
    }

    public MenuResult Run(IApplication app)
    {
        var menu = new MenuScreen("ADMIN", "TUIPOS Administration", _user, isInitialMenu: false);

        menu.AddOption(1, "Work with users", s => new WorkWithUsersScreen(_db, _user).Run(s.App));
        menu.AddOption(2, "Work with items", s => new WorkWithItemsScreen(_db).Run(s.App));
        menu.AddOption(3, "Change PIN", s =>
        {
            if (new ChangePinScreen(_db, _user).Run(s.App))
            {
                s.Message.Show(Messages.PinChanged);
            }
        });
        menu.AddOption(4, "Display system status", s => new SystemStatusScreen(_db).Run(s.App));

        return menu.Run(app);
    }
}
