using System.Globalization;
using System.Text;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace tuipos.UI;

// Shared colors and layout helpers for 5250-style screens.
public static class GreenScreen
{
    public const int Columns = 80;
    public const int Rows = 24;

    public const string SystemName = "TUIPOS";

    // The bottom two lines of the grid.
    public const int MessageRow = 22;
    public const int FunctionKeyRow = 23;

    public static readonly Scheme Normal = Uniform(Color.BrightGreen);
    public static readonly Scheme Bright = Uniform(Color.White);
    public static readonly Scheme FunctionKeys = Uniform(Color.BrightBlue);
    public static readonly Scheme Error = Uniform(Color.BrightRed);

    // Input fields are underlined, like 5250 entry fields.
    public static readonly Scheme Input = new()
    {
        Normal = Attr(Color.BrightGreen, TextStyle.Underline),
        Editable = Attr(Color.BrightGreen, TextStyle.Underline),
        Active = Attr(Color.White, TextStyle.Underline),
        Focus = Attr(Color.White, TextStyle.Underline),
        Highlight = Attr(Color.White, TextStyle.Reverse),
        ReadOnly = Attr(Color.BrightGreen),
        Disabled = Attr(Color.BrightGreen)
    };

    public static Label Text(int x, int y, string text, Scheme? scheme = null)
    {
        Label label = new()
        {
            X = x,
            Y = y,
            Text = text
        };

        if (scheme is not null)
        {
            label.SetScheme(scheme);
        }

        return label;
    }

    // An entry field that refuses edits longer than length, or with
    // characters that allowed rejects (e.g. char.IsAsciiDigit).
    // Monocase fields turn letters into capitals as they're typed.
    public static TextField InputField(
        int x,
        int y,
        int length,
        Func<char, bool>? allowed = null,
        bool monocase = false)
    {
        // One extra cell lets the cursor sit after the last character
        // without scrolling the first one out of view.
        TextField field = new()
        {
            X = x,
            Y = y,
            Width = length + 1
        };

        field.SetScheme(Input);

        field.TextChanging += (_, e) =>
        {
            string text = e.Result ?? "";
            e.Handled = text.Length > length
                || (allowed is not null && !text.All(allowed));

            if (!e.Handled && monocase)
            {
                e.Result = text.ToUpperInvariant();
            }
        };

        return field;
    }

    // IBM i object names, like user profiles, use letters, digits and $ # @ _.
    public static bool IsNameChar(char c)
    {
        return char.IsAsciiLetterOrDigit(c) || c is '$' or '#' or '@' or '_';
    }

    // The first character of a name can't be a digit or an underscore.
    public static bool IsValidName(string name)
    {
        return name.Length > 0
            && (char.IsAsciiLetter(name[0]) || name[0] is '$' or '#' or '@')
            && name.All(IsNameChar);
    }

    // $5.99 is stored as 599 and shown as "5.99".
    public static string Amount(long cents)
    {
        return (cents / 100m).ToString("0.00", CultureInfo.InvariantCulture);
    }

    // Reads a typed amount like "20" or "20.50" as cents.
    public static bool TryParseAmount(string text, out long cents)
    {
        cents = 0;

        if (!decimal.TryParse(
                text,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal amount)
            || decimal.Round(amount, 2) != amount)
        {
            return false;
        }

        cents = (long)(amount * 100);
        return true;
    }

    // Cuts text to fit a list column.
    public static string Clip(string text, int length)
    {
        return text.Length > length ? text[..length] : text;
    }

    // IBM i special values for yes/no fields.
    public static string YesNo(bool value)
    {
        return value ? "*YES" : "*NO";
    }

    // Accepts *YES, YES or Y and *NO, NO or N, in any case.
    public static bool? ParseYesNo(string text)
    {
        return text.Trim().TrimStart('*').ToUpperInvariant() switch
        {
            "YES" or "Y" => true,
            "NO" or "N" => false,
            _ => null
        };
    }

    // Pads a prompt with ". . ." out to width, e.g. "User  . . . . . .".
    // Dots sit on the same columns for every prompt of the same width.
    public static string DotLeader(string prompt, int width)
    {
        var text = new StringBuilder(prompt);

        for (int column = prompt.Length; column < width; column++)
        {
            bool isDotColumn = (width - 1 - column) % 2 == 0;
            text.Append(isDotColumn && column > prompt.Length ? '.' : ' ');
        }

        return text.ToString();
    }

    // A read-only "Prompt . . . . :   value" line.
    public static View OutputLine(
        int x,
        int y,
        string prompt,
        int promptWidth,
        string value)
    {
        // Sized to its text so it can't draw over anything to its right,
        // like a pop-up window's border.
        View line = new()
        {
            X = x,
            Y = y,
            Width = promptWidth + 5 + value.Length,
            Height = 1
        };

        line.Add(
            Text(0, 0, DotLeader(prompt, promptWidth) + " :"),
            Text(promptWidth + 5, 0, value, Bright)
        );

        return line;
    }

    // IBM i style date and time, e.g. "10/27/26  17:45:12".
    public static string Timestamp(DateTime value)
    {
        return value.ToString("MM/dd/yy  HH:mm:ss", CultureInfo.InvariantCulture);
    }

    // Screen ID on the left, title centered, system name on the right.
    public static void AddHeader(View grid, string screenId, string title)
    {
        Label titleLabel = Text(0, 0, title, Bright);
        titleLabel.X = Pos.Center();

        grid.Add(
            Text(1, 0, screenId),
            titleLabel,
            Text(56, 1, "System:"),
            Text(66, 1, SystemName, Bright)
        );
    }

    private static Terminal.Gui.Drawing.Attribute Attr(
        Color foreground,
        TextStyle style = TextStyle.None)
    {
        return new Terminal.Gui.Drawing.Attribute(foreground, Color.Black, style);
    }

    private static Scheme Uniform(Color foreground)
    {
        var attribute = Attr(foreground);

        return new Scheme
        {
            Normal = attribute,
            HotNormal = attribute,
            Focus = attribute,
            HotFocus = attribute,
            Active = attribute,
            HotActive = attribute,
            Highlight = attribute,
            Editable = attribute,
            ReadOnly = attribute,
            Disabled = attribute
        };
    }
}
