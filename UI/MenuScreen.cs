using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Models;

namespace tuipos.UI;

public enum MenuResult
{
    // Back to the previous menu.
    Exit,

    // All the way back to the sign-on screen.
    SignOff
}

// Passed to an option's handler when it's chosen.
public class MenuSelection
{
    public MenuSelection(IApplication app, MessageLine message, int option)
    {
        App = app;
        Message = message;
        Option = option;
    }

    public IApplication App { get; }
    public MessageLine Message { get; }
    public int Option { get; }

    // Set when a submenu signs off, so this menu closes too.
    public bool SignOff { get; set; }
}

// An IBM i style numbered menu with a command line. The initial menu
// (MAIN) signs off on F3. Submenus go back to the previous menu on F3
// or F12, and sign off with option 90 or the SIGNOFF command.
public class MenuScreen
{
    private const int SignOffOption = 90;

    private readonly string _menuId;
    private readonly string _title;
    private readonly PosUser _user;
    private readonly bool _isInitialMenu;

    private readonly List<(int Number, string Text, Action<MenuSelection> Select)> _options = new();

    public MenuScreen(string menuId, string title, PosUser user, bool isInitialMenu)
    {
        _menuId = menuId;
        _title = title;
        _user = user;
        _isInitialMenu = isInitialMenu;
    }

    public void AddOption(int number, string text, Action<MenuSelection> select)
    {
        _options.Add((number, text, select));
    }

    public MenuResult Run(IApplication app)
    {
        var result = MenuResult.Exit;

        using GreenScreenWindow window = new("F3=Exit   F9=Retrieve   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, _menuId, _title);

        TextField commandField = GreenScreen.InputField(6, 20, 72);

        // Commands entered on this menu, for F9=Retrieve.
        var history = new List<string>();
        int historyIndex = 0;

        void SignOff()
        {
            result = MenuResult.SignOff;
            app.RequestStop();
        }

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
                SignOff();
                return;
            }

            if (!int.TryParse(entry, out int number))
            {
                // Leave the entry in place so it can be corrected.
                window.Message.Error(Messages.SelectionNotValid);
                return;
            }

            if (number == SignOffOption)
            {
                SignOff();
                return;
            }

            var option = _options.FirstOrDefault(o => o.Number == number);

            if (option.Select is null)
            {
                window.Message.Error(Messages.SelectionNotValid);
                return;
            }

            commandField.Text = "";

            var selection = new MenuSelection(app, window.Message, number);
            option.Select(selection);

            if (selection.SignOff)
            {
                SignOff();
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

                if (_isInitialMenu)
                {
                    SignOff();
                }
                else
                {
                    app.RequestStop();
                }
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
                // Esc acts like F12 so a stray press never signs off.
                key.Handled = true;

                if (_isInitialMenu)
                {
                    commandField.Text = "";
                    window.Message.Clear();
                }
                else
                {
                    app.RequestStop();
                }
            }
        };

        window.Grid.Add(
            GreenScreen.Text(56, 2, "User:"),
            GreenScreen.Text(66, 2, _user.UserId, GreenScreen.Bright),
            GreenScreen.Text(1, 3, "Select one of the following:")
        );

        int row = 5;

        foreach (var option in _options)
        {
            window.Grid.Add(OptionLine(row++, option.Number, option.Text));
        }

        // A blank line sets Sign off apart, as on IBM i menus.
        window.Grid.Add(
            OptionLine(row + 1, SignOffOption, "Sign off"),
            GreenScreen.Text(1, 19, "Selection or command"),
            GreenScreen.Text(1, 20, "===>", GreenScreen.Bright),
            commandField
        );

        window.RunWithClock(app);

        return result;
    }

    // A numbered menu line, e.g. "     5. Display user information".
    private static Label OptionLine(int y, int number, string text)
    {
        return GreenScreen.Text(5, y, $"{number,2}. {text}");
    }
}
