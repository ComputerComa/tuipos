using Microsoft.EntityFrameworkCore;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

// Administration option 3: the signed-on user changes their own password,
// like IBM i's Change Password screen.
public class ChangePasswordScreen
{
    private const int PromptX = 2;
    private const int PromptWidth = 28;
    private const int FieldX = 33;

    private readonly PosDbContext _db;
    private readonly PosUser _user;

    public ChangePasswordScreen(PosDbContext db, PosUser user)
    {
        _db = db;
        _user = user;
    }

    // Returns true if the password was changed.
    public bool Run(IApplication app)
    {
        bool changed = false;

        using GreenScreenWindow window = new("F3=Exit   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "CHGPWD", "Change Password");

        TextField currentField = GreenScreen.InputField(FieldX, 5, Passwords.MaxLength, Passwords.IsAllowed);
        TextField newField = GreenScreen.InputField(FieldX, 6, Passwords.MaxLength, Passwords.IsAllowed);
        TextField verifyField = GreenScreen.InputField(FieldX, 7, Passwords.MaxLength, Passwords.IsAllowed);

        currentField.Secret = true;
        newField.Secret = true;
        verifyField.Secret = true;

        void Fail(TextField focus, string message, params TextField[] clear)
        {
            foreach (var field in clear)
            {
                field.Text = "";
            }

            window.Message.Error(message);
            focus.SetFocus();
        }

        void TryChange()
        {
            window.Message.Clear();

            // Passwords aren't case-sensitive, so compare them uppercased.
            string currentPassword = (currentField.Text ?? "").ToUpperInvariant();
            string newPassword = (newField.Text ?? "").ToUpperInvariant();
            string verifyPassword = (verifyField.Text ?? "").ToUpperInvariant();

            // Read the stored hash fresh; the signed-on snapshot may be out of date.
            var user = _db.Users.AsNoTracking().First(u => u.Id == _user.Id);

            if (!Passwords.Verify(user, currentPassword))
            {
                Fail(currentField, Messages.CurrentPasswordNotCorrect, currentField);
                return;
            }

            if (!Passwords.IsValid(newPassword))
            {
                Fail(newField, Messages.NewPasswordNotValid, newField, verifyField);
                return;
            }

            if (newPassword != verifyPassword)
            {
                Fail(newField, Messages.NewPasswordsDontMatch, newField, verifyField);
                return;
            }

            if (newPassword == currentPassword)
            {
                Fail(newField, Messages.NewPasswordSameAsCurrent, newField, verifyField);
                return;
            }

            string newHash = Passwords.Hash(user, newPassword);

            _db.Users
                .Where(u => u.Id == _user.Id)
                .ExecuteUpdate(setters => setters.SetProperty(u => u.PinHash, newHash));

            changed = true;
            app.RequestStop();
        }

        foreach (var field in new[] { currentField, newField, verifyField })
        {
            field.Accepting += (_, e) =>
            {
                e.Handled = true;
                TryChange();
            };
        }

        window.KeyDown += (_, key) =>
        {
            if (key == Key.F3 || key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                app.RequestStop();
            }
        };

        window.Grid.Add(
            GreenScreen.Text(1, 3, "Type choices, press Enter."),
            GreenScreen.Text(PromptX, 5, GreenScreen.DotLeader("Current password", PromptWidth)),
            currentField,
            GreenScreen.Text(PromptX, 6, GreenScreen.DotLeader("New password", PromptWidth)),
            newField,
            GreenScreen.Text(PromptX, 7, GreenScreen.DotLeader("New password (to verify)", PromptWidth)),
            verifyField
        );

        app.Run(window);

        return changed;
    }
}
