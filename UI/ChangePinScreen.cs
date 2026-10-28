using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

// Administration option 3: the signed-on user changes their own PIN,
// like IBM i's Change Password screen.
public class ChangePinScreen
{
    private const int PinLength = 6;

    private const int PromptX = 2;
    private const int PromptWidth = 28;
    private const int FieldX = 33;

    private readonly PosDbContext _db;
    private readonly PosUser _user;

    public ChangePinScreen(PosDbContext db, PosUser user)
    {
        _db = db;
        _user = user;
    }

    // Returns true if the PIN was changed.
    public bool Run(IApplication app)
    {
        bool changed = false;

        using GreenScreenWindow window = new("F3=Exit   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "CHGPIN", "Change PIN");

        TextField currentField = GreenScreen.InputField(FieldX, 5, PinLength, char.IsAsciiDigit);
        TextField newField = GreenScreen.InputField(FieldX, 6, PinLength, char.IsAsciiDigit);
        TextField verifyField = GreenScreen.InputField(FieldX, 7, PinLength, char.IsAsciiDigit);

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

            string currentPin = currentField.Text ?? "";
            string newPin = newField.Text ?? "";
            string verifyPin = verifyField.Text ?? "";

            // Read the stored hash fresh; the signed-on snapshot may be out of date.
            var user = _db.Users.AsNoTracking().First(u => u.Id == _user.Id);
            var hasher = new PasswordHasher<PosUser>();

            if (hasher.VerifyHashedPassword(user, user.PinHash, currentPin)
                == PasswordVerificationResult.Failed)
            {
                Fail(currentField, Messages.CurrentPinNotCorrect, currentField);
                return;
            }

            if (newPin.Length != PinLength)
            {
                Fail(newField, Messages.NewPinNotValid, newField, verifyField);
                return;
            }

            if (newPin != verifyPin)
            {
                Fail(newField, Messages.NewPinsDontMatch, newField, verifyField);
                return;
            }

            if (newPin == currentPin)
            {
                Fail(newField, Messages.NewPinSameAsCurrent, newField, verifyField);
                return;
            }

            string newHash = hasher.HashPassword(user, newPin);

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
            GreenScreen.Text(PromptX, 5, GreenScreen.DotLeader("Current PIN", PromptWidth)),
            currentField,
            GreenScreen.Text(PromptX, 6, GreenScreen.DotLeader("New PIN", PromptWidth)),
            newField,
            GreenScreen.Text(PromptX, 7, GreenScreen.DotLeader("New PIN (to verify)", PromptWidth)),
            verifyField
        );

        app.Run(window);

        return changed;
    }
}
