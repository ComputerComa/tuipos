using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

// CRTUSR and CHGUSR: create a user, or change one picked on WRKUSR.
public class UserEditScreen
{
    private const int UserIdLength = 10;
    private const int NameLength = 20;

    private const int PromptX = 2;
    private const int PromptWidth = 28;
    private const int FieldX = 33;
    private const int HintX = 45;

    private readonly PosDbContext _db;
    private readonly PosUser? _existing;

    // Pass null to create a new user.
    public UserEditScreen(PosDbContext db, PosUser? existing)
    {
        _db = db;
        _existing = existing;
    }

    public string SavedUserId { get; private set; } = "";

    // Returns true if the user was saved.
    public bool Run(IApplication app)
    {
        bool saved = false;
        bool creating = _existing is null;

        using GreenScreenWindow window = new("F3=Exit   F12=Cancel");

        GreenScreen.AddHeader(
            window.Grid,
            creating ? "CRTUSR" : "CHGUSR",
            creating ? "Create User" : "Change User"
        );

        TextField userIdField = GreenScreen.InputField(FieldX, 5, UserIdLength, GreenScreen.IsNameChar, monocase: true);
        TextField firstNameField = GreenScreen.InputField(FieldX, 6, NameLength);
        TextField lastNameField = GreenScreen.InputField(FieldX, 7, NameLength);
        TextField passwordField = GreenScreen.InputField(FieldX, 8, Passwords.MaxLength, Passwords.IsAllowed);
        TextField adminField = GreenScreen.InputField(FieldX, 9, 4, c => char.IsAsciiLetter(c) || c == '*');

        passwordField.Secret = true;

        firstNameField.Text = _existing?.FirstName ?? "";
        lastNameField.Text = _existing?.LastName ?? "";
        adminField.Text = GreenScreen.YesNo(_existing?.IsAdmin ?? false);

        void Fail(TextField field, string message)
        {
            window.Message.Error(message);
            field.SetFocus();
        }

        void Save()
        {
            window.Message.Clear();

            string userId = _existing?.UserId ?? (userIdField.Text ?? "").Trim().ToUpperInvariant();
            string firstName = (firstNameField.Text ?? "").Trim();
            string lastName = (lastNameField.Text ?? "").Trim();
            string password = passwordField.Text ?? "";
            bool? isAdmin = GreenScreen.ParseYesNo(adminField.Text ?? "");

            if (creating && userId.Length == 0)
            {
                Fail(userIdField, Messages.NewUserIdRequired);
                return;
            }

            if (creating && !GreenScreen.IsValidName(userId))
            {
                Fail(userIdField, Messages.UserProfileNotValid);
                return;
            }

            // Users created before profiles were uppercase may be stored in lowercase.
            if (creating && _db.Users.Any(u => u.UserId.ToUpper() == userId))
            {
                Fail(userIdField, Messages.UserExists(userId));
                return;
            }

            if (firstName.Length == 0)
            {
                Fail(firstNameField, Messages.FirstNameRequired);
                return;
            }

            if (lastName.Length == 0)
            {
                Fail(lastNameField, Messages.LastNameRequired);
                return;
            }

            // A new user needs a password; a blank one on Change keeps the old one.
            if ((creating || password.Length > 0) && !Passwords.IsValid(password))
            {
                passwordField.Text = "";
                Fail(passwordField, Messages.PasswordNotValid);
                return;
            }

            if (isAdmin is null)
            {
                Fail(adminField, Messages.AdminNotValid);
                return;
            }

            if (_existing is { IsAdmin: true }
                && isAdmin == false
                && !_db.Users.Any(u => u.IsAdmin && u.Id != _existing.Id))
            {
                Fail(adminField, Messages.LastAdmin);
                return;
            }

            var user = _existing is null
                ? new PosUser { UserId = userId }
                : _db.Users.First(u => u.Id == _existing.Id);

            user.FirstName = firstName;
            user.LastName = lastName;
            user.IsAdmin = isAdmin.Value;

            if (password.Length > 0)
            {
                user.PinHash = Passwords.Hash(user, password);
            }

            if (creating)
            {
                _db.Users.Add(user);
            }

            _db.SaveChanges();

            // The context lives as long as the app, so don't keep entities
            // tracked between screens.
            _db.ChangeTracker.Clear();

            SavedUserId = userId;
            saved = true;
            app.RequestStop();
        }

        foreach (var field in new[] { userIdField, firstNameField, lastNameField, passwordField, adminField })
        {
            field.Accepting += (_, e) =>
            {
                e.Handled = true;
                Save();
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
            GreenScreen.Text(PromptX, 5, GreenScreen.DotLeader("User profile", PromptWidth))
        );

        // The user profile is the key, so Change shows it but doesn't let it change.
        if (creating)
        {
            window.Grid.Add(userIdField);
        }
        else
        {
            window.Grid.Add(GreenScreen.Text(FieldX, 5, _existing!.UserId, GreenScreen.Bright));
        }

        window.Grid.Add(
            GreenScreen.Text(PromptX, 6, GreenScreen.DotLeader("First name", PromptWidth)),
            firstNameField,
            GreenScreen.Text(PromptX, 7, GreenScreen.DotLeader("Last name", PromptWidth)),
            lastNameField,
            GreenScreen.Text(PromptX, 8, GreenScreen.DotLeader(creating ? "Password" : "New password", PromptWidth)),
            passwordField,
            GreenScreen.Text(HintX, 8, creating ? "6 to 10 characters" : "Leave blank to keep", GreenScreen.FunctionKeys),
            GreenScreen.Text(PromptX, 9, GreenScreen.DotLeader("Administrator", PromptWidth)),
            adminField,
            GreenScreen.Text(HintX, 9, "*YES, *NO", GreenScreen.FunctionKeys)
        );

        app.Run(window);

        return saved;
    }
}
