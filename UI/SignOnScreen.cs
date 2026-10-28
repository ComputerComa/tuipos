using Microsoft.EntityFrameworkCore;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

public class SignOnScreen
{
    private const int UserIdLength = 10;

    // Prompts end at column 49 and fields start at column 53.
    private const int PromptX = 17;
    private const int PromptWidth = 33;
    private const int FieldX = 53;

    private readonly PosDbContext _db;

    public SignOnScreen(PosDbContext db)
    {
        _db = db;
    }

    // Returns the signed-on user, or null if the user exits.
    public PosUser? Run(IApplication app)
    {
        PosUser? signedOnUser = null;

        using GreenScreenWindow window = new("F3=Exit");

        Label title = GreenScreen.Text(0, 0, "Sign On", GreenScreen.Bright);
        title.X = Pos.Center();

        TextField userIdField = GreenScreen.InputField(FieldX, 7, UserIdLength, monocase: true);
        TextField passwordField = GreenScreen.InputField(FieldX, 8, Passwords.MaxLength, Passwords.IsAllowed);
        passwordField.Secret = true;

        void TrySignOn()
        {
            string userId = (userIdField.Text ?? "").Trim().ToUpperInvariant();
            string password = passwordField.Text ?? "";

            // Clear the password field after each attempt.
            passwordField.Text = "";

            if (userId.Length == 0)
            {
                window.Message.Error(Messages.UserIdRequired);
                userIdField.SetFocus();
                return;
            }

            // Users created before profiles were uppercase may be stored in lowercase.
            var user = _db.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.UserId.ToUpper() == userId);

            if (user is null)
            {
                window.Message.Error(Messages.UserNotFound(userId));
                userIdField.SetFocus();
                return;
            }

            if (!Passwords.Verify(user, password))
            {
                window.Message.Error(Messages.PasswordNotCorrect(userId));
                passwordField.SetFocus();
                return;
            }

            signedOnUser = user;
            app.RequestStop();
        }

        userIdField.Accepting += (_, e) =>
        {
            e.Handled = true;

            // Enter on a filled-in user moves to the password like Field Exit.
            if ((passwordField.Text ?? "").Length == 0
                && (userIdField.Text ?? "").Trim().Length > 0)
            {
                window.Message.Clear();
                passwordField.SetFocus();
                return;
            }

            TrySignOn();
        };

        passwordField.Accepting += (_, e) =>
        {
            e.Handled = true;
            TrySignOn();
        };

        window.KeyDown += (_, key) =>
        {
            if (key == Key.F3)
            {
                key.Handled = true;
                app.RequestStop();
            }
        };

        window.Grid.Add(
            title,
            GreenScreen.OutputLine(48, 2, "System", 17, GreenScreen.SystemName),
            GreenScreen.OutputLine(48, 3, "Subsystem", 17, "QPOS"),
            GreenScreen.OutputLine(48, 4, "Display", 17, "QPADEV0001"),
            GreenScreen.Text(PromptX, 7, GreenScreen.DotLeader("User", PromptWidth)),
            GreenScreen.Text(PromptX, 8, GreenScreen.DotLeader("Password", PromptWidth)),
            userIdField,
            passwordField,
            GreenScreen.Text(53, 20, "(C) COPYRIGHT TUIPOS 2026.")
        );

        app.Run(window);

        return signedOnUser;
    }
}
