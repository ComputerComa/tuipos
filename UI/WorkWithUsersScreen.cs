using Microsoft.EntityFrameworkCore;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

// Administration option 1: list users, with 2=Change, 4=Delete and F6=Create.
public class WorkWithUsersScreen
{
    private const int FirstRow = 7;
    private const int RowsPerPage = 12;

    private static readonly string Headings =
        $"{"User ID",-10}  {"First name",-20}  {"Last name",-20}  Admin";

    private readonly PosDbContext _db;
    private readonly PosUser _signedOnUser;

    public WorkWithUsersScreen(PosDbContext db, PosUser signedOnUser)
    {
        _db = db;
        _signedOnUser = signedOnUser;
    }

    public void Run(IApplication app)
    {
        using GreenScreenWindow window = new("F3=Exit   F5=Refresh   F6=Create   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "WRKUSR", "Work with Users");

        var list = new OptionList<PosUser>(FirstRow, RowsPerPage, FormatUser, "(No users)");

        void Load()
        {
            list.SetItems(_db.Users.AsNoTracking().OrderBy(u => u.UserId).ToList());
        }

        void Submit()
        {
            window.Message.Clear();

            var typed = list.TypedOptions();

            foreach (var (_, option, field) in typed)
            {
                if (option != "2" && option != "4")
                {
                    window.Message.Error(Messages.OptionNotValid(option));
                    field.SetFocus();
                    return;
                }
            }

            string? info = null;
            string? error = null;

            // Like a subfile program, handle each option in order down the page.
            int changed = 0;

            foreach (var (user, option, _) in typed)
            {
                if (option == "2" && new UserEditScreen(_db, user).Run(app))
                {
                    changed++;
                }
            }

            if (changed > 0)
            {
                info = Messages.UsersChanged(changed);
            }

            var toDelete = typed.Where(t => t.Option == "4").Select(t => t.Item).ToList();

            if (toDelete.Count > 0 && ConfirmDelete(app, toDelete))
            {
                int deleted = 0;

                foreach (var user in toDelete)
                {
                    string? problem = DeleteProblem(user);

                    if (problem is not null)
                    {
                        error ??= problem;
                        continue;
                    }

                    _db.Users.Where(u => u.Id == user.Id).ExecuteDelete();
                    deleted++;
                }

                if (deleted > 0)
                {
                    info = Messages.UsersDeleted(deleted);
                }
            }

            Load();

            if (error is not null)
            {
                window.Message.Error(error);
            }
            else if (info is not null)
            {
                window.Message.Show(info);
            }
        }

        list.OnEnter(Submit);

        window.KeyDown += (_, key) =>
        {
            if (key == Key.Enter)
            {
                // Enter with no field focused, e.g. an empty list.
                key.Handled = true;
                Submit();
            }
            else if (key == Key.F3 || key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                app.RequestStop();
            }
            else if (key == Key.F5)
            {
                key.Handled = true;
                window.Message.Clear();
                Load();
            }
            else if (key == Key.F6)
            {
                key.Handled = true;
                var editor = new UserEditScreen(_db, null);

                if (editor.Run(app))
                {
                    Load();
                    window.Message.Show(Messages.UserCreated(editor.SavedUserId));
                }
            }
            else if (key == Key.PageDown)
            {
                key.Handled = true;
                list.PageDown();
            }
            else if (key == Key.PageUp)
            {
                key.Handled = true;
                list.PageUp();
            }
        };

        window.Grid.Add(
            GreenScreen.Text(1, 3, "Type options, press Enter."),
            GreenScreen.Text(3, 4, "2=Change   4=Delete", GreenScreen.FunctionKeys),
            GreenScreen.Text(1, 6, "Opt", GreenScreen.Bright),
            GreenScreen.Text(7, 6, Headings, GreenScreen.Bright)
        );

        list.AddTo(window.Grid);
        Load();

        app.Run(window);
    }

    private bool ConfirmDelete(IApplication app, List<PosUser> users)
    {
        var rows = users.Select(FormatUser).ToList();
        return new ConfirmDeleteScreen("Confirm Delete of Users", Headings, rows).Run(app);
    }

    // Why a user can't be deleted, or null if it can.
    private string? DeleteProblem(PosUser user)
    {
        if (user.Id == _signedOnUser.Id)
        {
            return Messages.CantDeleteSelf(user.UserId);
        }

        // Sales keep a link to the user who rang them up.
        if (_db.Sales.Any(s => s.UserId == user.Id))
        {
            return Messages.UserHasSales(user.UserId);
        }

        if (user.IsAdmin && !_db.Users.Any(u => u.IsAdmin && u.Id != user.Id))
        {
            return Messages.LastAdmin;
        }

        return null;
    }

    private static string FormatUser(PosUser user)
    {
        return $"{user.UserId,-10}  {GreenScreen.Clip(user.FirstName, 20),-20}  "
            + $"{GreenScreen.Clip(user.LastName, 20),-20}  {GreenScreen.YesNo(user.IsAdmin)}";
    }
}
