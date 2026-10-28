using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace tuipos.UI;

// A borderless black window with a centered 80x24 grid. Screens add their
// views to Grid using absolute 5250 row and column positions.
public class GreenScreenWindow : Window
{
    public View Grid { get; }
    public MessageLine Message { get; }

    public GreenScreenWindow(string functionKeys)
    {
        BorderStyle = LineStyle.None;
        SetScheme(GreenScreen.Normal);

        Grid = new View
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = GreenScreen.Columns,
            Height = GreenScreen.Rows,
            CanFocus = true
        };

        Message = new MessageLine();

        Grid.Add(
            Message,
            GreenScreen.Text(
                1,
                GreenScreen.FunctionKeyRow,
                functionKeys,
                GreenScreen.FunctionKeys
            )
        );

        Add(Grid);
    }

    // Runs the screen with the date and time in the top-right corner,
    // updated every second.
    public void RunWithClock(IApplication app)
    {
        Label clock = GreenScreen.Text(61, 0, GreenScreen.Timestamp(DateTime.Now));
        Grid.Add(clock);

        object? timer = app.AddTimeout(TimeSpan.FromSeconds(1), () =>
        {
            clock.Text = GreenScreen.Timestamp(DateTime.Now);
            return true;
        });

        app.Run(this);

        if (timer is not null)
        {
            app.RemoveTimeout(timer);
        }
    }
}
