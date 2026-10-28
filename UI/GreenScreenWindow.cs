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
}
