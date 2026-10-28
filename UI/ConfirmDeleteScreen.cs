using Terminal.Gui.App;
using Terminal.Gui.Input;

namespace tuipos.UI;

// IBM i style confirmation listing the rows marked with 4=Delete.
public class ConfirmDeleteScreen
{
    private readonly string _title;
    private readonly string _headings;
    private readonly IReadOnlyList<string> _rows;

    public ConfirmDeleteScreen(string title, string headings, IReadOnlyList<string> rows)
    {
        _title = title;
        _headings = headings;
        _rows = rows;
    }

    // Returns true if the user pressed Enter to confirm.
    public bool Run(IApplication app)
    {
        bool confirmed = false;

        using GreenScreenWindow window = new("F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "", _title);

        window.KeyDown += (_, key) =>
        {
            if (key == Key.Enter)
            {
                key.Handled = true;
                confirmed = true;
                app.RequestStop();
            }
            else if (key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                app.RequestStop();
            }
        };

        window.Grid.Add(
            GreenScreen.Text(1, 3, "Press Enter to confirm your choices for 4=Delete."),
            GreenScreen.Text(1, 4, "Press F12=Cancel to go back without deleting."),
            GreenScreen.Text(1, 6, "Opt", GreenScreen.Bright),
            GreenScreen.Text(7, 6, _headings, GreenScreen.Bright)
        );

        // Only one page of options can be typed at a time, so the rows fit.
        for (int i = 0; i < _rows.Count; i++)
        {
            window.Grid.Add(
                GreenScreen.Text(2, 7 + i, "4"),
                GreenScreen.Text(7, 7 + i, _rows[i])
            );
        }

        app.Run(window);

        return confirmed;
    }
}
