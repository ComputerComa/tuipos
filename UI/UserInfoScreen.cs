using Terminal.Gui.App;
using Terminal.Gui.Input;
using tuipos.Models;

namespace tuipos.UI;

// Main menu option 5: details about the signed-on user.
public class UserInfoScreen
{
    private const int PromptWidth = 31;

    private readonly PosUser _user;
    private readonly DateTime _signedOnAt;

    public UserInfoScreen(PosUser user, DateTime signedOnAt)
    {
        _user = user;
        _signedOnAt = signedOnAt;
    }

    public void Run(IApplication app)
    {
        using GreenScreenWindow window = new("F3=Exit   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "DSPUSR", "Display User Information");

        window.KeyDown += (_, key) =>
        {
            if (key == Key.Enter || key == Key.F3 || key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                app.RequestStop();
            }
        };

        window.Grid.Add(
            GreenScreen.OutputLine(1, 4, "User ID", PromptWidth, _user.UserId),
            GreenScreen.OutputLine(1, 5, "First name", PromptWidth, _user.FirstName),
            GreenScreen.OutputLine(1, 6, "Last name", PromptWidth, _user.LastName),
            GreenScreen.OutputLine(
                1,
                7,
                "Signed on",
                PromptWidth,
                GreenScreen.Timestamp(_signedOnAt)
            ),
            GreenScreen.Text(1, 20, "Press Enter to continue.")
        );

        app.Run(window);
    }
}
