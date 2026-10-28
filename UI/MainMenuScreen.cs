using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Models;

namespace tuipos.UI;

public class MainMenuScreen
{
    private const int SignOffOption = 90;

    private readonly PosUser _user;

    public MainMenuScreen(PosUser user)
    {
        _user = user;
    }

    // Returns when the user signs off.
    public void Run(IApplication app)
    {
        var signedOnAt = DateTime.Now;

        using GreenScreenWindow window = new("F3=Exit   F9=Retrieve   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "MAIN", "TUIPOS Main Menu");

        Label clock = GreenScreen.Text(61, 0, GreenScreen.Timestamp(signedOnAt));

        TextField commandField = GreenScreen.InputField(6, 20, 72);

        // Commands entered this session, for F9=Retrieve.
        var history = new List<string>();
        int historyIndex = 0;

        void ProcessCommand()
        {
            string entry = (commandField.Text ?? "").Trim();

            window.Message.Clear();

            if (entry.Length == 0)
            {
                return;
            }

            history.Add(entry);
            historyIndex = history.Count;

            if (entry.Equals("SIGNOFF", StringComparison.OrdinalIgnoreCase))
            {
                app.RequestStop();
                return;
            }

            if (!int.TryParse(entry, out int option))
            {
                // Leave the entry in place so it can be corrected.
                window.Message.Error(Messages.SelectionNotValid);
                return;
            }

            switch (option)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    commandField.Text = "";
                    window.Message.Show(Messages.OptionNotAvailable(option));
                    break;

                case 5:
                    commandField.Text = "";
                    new UserInfoScreen(_user, signedOnAt).Run(app);
                    break;

                case SignOffOption:
                    app.RequestStop();
                    break;

                default:
                    window.Message.Error(Messages.SelectionNotValid);
                    break;
            }
        }

        commandField.Accepting += (_, e) =>
        {
            e.Handled = true;
            ProcessCommand();
        };

        window.KeyDown += (_, key) =>
        {
            if (key == Key.F3)
            {
                key.Handled = true;
                app.RequestStop();
            }
            else if (key == Key.F9)
            {
                key.Handled = true;

                if (history.Count > 0)
                {
                    historyIndex = Math.Max(0, historyIndex - 1);
                    commandField.Text = history[historyIndex];
                    commandField.MoveEnd();
                }
            }
            else if (key == Key.F12 || key == Key.Esc)
            {
                // Esc acts like F12 so a stray press doesn't sign off.
                key.Handled = true;
                commandField.Text = "";
                window.Message.Clear();
            }
        };

        window.Grid.Add(
            clock,
            GreenScreen.Text(56, 2, "User:"),
            GreenScreen.Text(66, 2, _user.UserId, GreenScreen.Bright),
            GreenScreen.Text(1, 3, "Select one of the following:"),
            Option(5, 1, "Register"),
            Option(6, 2, "Item inquiry"),
            Option(7, 3, "Sales journal"),
            Option(8, 4, "Daily totals"),
            Option(9, 5, "Display user information"),
            Option(11, SignOffOption, "Sign off"),
            GreenScreen.Text(1, 19, "Selection or command"),
            GreenScreen.Text(1, 20, "===>", GreenScreen.Bright),
            commandField
        );

        object? clockTimer = app.AddTimeout(TimeSpan.FromSeconds(1), () =>
        {
            clock.Text = GreenScreen.Timestamp(DateTime.Now);
            return true;
        });

        app.Run(window);

        if (clockTimer is not null)
        {
            app.RemoveTimeout(clockTimer);
        }
    }

    // A numbered menu line, e.g. "     5. Display user information".
    private static Label Option(int y, int number, string text)
    {
        return GreenScreen.Text(5, y, $"{number,2}. {text}");
    }
}
