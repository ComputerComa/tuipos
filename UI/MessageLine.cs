using Terminal.Gui.Views;

namespace tuipos.UI;

// The message line just above the function keys.
public class MessageLine : Label
{
    public MessageLine()
    {
        X = 1;
        Y = GreenScreen.MessageRow;
        Width = GreenScreen.Columns - 2;
    }

    public void Show(string text)
    {
        SetScheme(GreenScreen.Bright);
        Text = text;
    }

    public void Error(string text)
    {
        SetScheme(GreenScreen.Error);
        Text = text;
    }

    public void Clear()
    {
        Text = "";
    }
}
