using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Reports;

namespace tuipos.UI;

// DSPSPLF: read a spooled file on screen. Reports are 132 columns wide, so
// the screen shows 78 at a time and shifts left and right like IBM's
// F19=Left and F20=Right. 5250 emulators put F13-F24 on Shift+F1-F12, so
// Shift+F7 and Shift+F8 work too, as do the arrow keys.
public class DisplaySpooledFileScreen
{
    private const int FirstRow = 5;
    private const int VisibleRows = 16;
    private const int VisibleColumns = 78;
    private const int ShiftStep = 40;
    private const int MaxShift = ReportWriter.Width - VisibleColumns;

    private readonly SpooledFile _file;

    public DisplaySpooledFileScreen(SpooledFile file)
    {
        _file = file;
    }

    public void Run(IApplication app)
    {
        var lines = ReadLines(_file.Path);
        int top = 0;
        int shift = 0;

        using GreenScreenWindow window = new("F3=Exit   F12=Cancel   F19=Left   F20=Right");

        GreenScreen.AddHeader(window.Grid, "DSPSPLF", "Display Spooled File");

        Label pageLine = GreenScreen.Text(66, 2, "", GreenScreen.Bright);
        pageLine.Width = 12;

        Label columns = GreenScreen.Text(20, 3, "", GreenScreen.Bright);
        columns.Width = 12;

        var rows = new Label[VisibleRows];

        for (int i = 0; i < VisibleRows; i++)
        {
            rows[i] = GreenScreen.Text(1, FirstRow + i, "");
            rows[i].Width = VisibleColumns;
        }

        Label more = GreenScreen.Text(70, FirstRow + VisibleRows, "");
        more.Width = 9;

        void Show()
        {
            for (int i = 0; i < VisibleRows; i++)
            {
                int index = top + i;
                string text = index < lines.Count ? lines[index].Text : "";

                rows[i].Text = text.Length > shift
                    ? GreenScreen.Clip(text[shift..], VisibleColumns)
                    : "";
            }

            var first = lines.Count > 0 ? lines[top] : (Page: 1, Line: 1, Text: "");

            pageLine.Text = $"{first.Page}/{first.Line}";
            columns.Text = $"{shift + 1} - {shift + VisibleColumns}";
            more.Text = (top + VisibleRows < lines.Count ? "More..." : "Bottom").PadLeft(9);
        }

        void ShiftBy(int columnsToMove)
        {
            shift = Math.Clamp(shift + columnsToMove, 0, MaxShift);
            Show();
        }

        void ScrollTo(int line)
        {
            top = Math.Clamp(line, 0, Math.Max(0, lines.Count - 1));
            Show();
        }

        window.KeyDown += (_, key) =>
        {
            if (key == Key.Enter || key == Key.F3 || key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                app.RequestStop();
                return;
            }

            key.Handled = true;

            if (key == Key.F19 || key == Key.F7.WithShift || key == Key.CursorLeft)
            {
                ShiftBy(-ShiftStep);
            }
            else if (key == Key.F20 || key == Key.F8.WithShift || key == Key.CursorRight)
            {
                ShiftBy(ShiftStep);
            }
            else if (key == Key.PageDown)
            {
                if (top + VisibleRows < lines.Count)
                {
                    ScrollTo(top + VisibleRows);
                }
            }
            else if (key == Key.PageUp)
            {
                ScrollTo(top - VisibleRows);
            }
            else if (key == Key.CursorDown)
            {
                ScrollTo(top + 1);
            }
            else if (key == Key.CursorUp)
            {
                ScrollTo(top - 1);
            }
            else if (key == Key.Home)
            {
                ScrollTo(0);
            }
            else if (key == Key.End)
            {
                ScrollTo(lines.Count - VisibleRows);
            }
            else
            {
                key.Handled = false;
            }
        };

        window.Grid.Add(
            GreenScreen.Text(1, 2, GreenScreen.DotLeader("File", 15) + " :"),
            GreenScreen.Text(20, 2, $"{_file.Name}   Nbr {_file.Number}", GreenScreen.Bright),
            GreenScreen.Text(56, 2, "Page/Line"),
            pageLine,
            GreenScreen.Text(1, 3, GreenScreen.DotLeader("Columns", 15) + " :"),
            columns
        );

        window.Grid.Add(rows);
        window.Grid.Add(more);

        Show();
        app.Run(window);
    }

    // Each line with its page and line number. Pages end in a form feed and
    // lines in CRLF, the way the printer wants them.
    private static List<(int Page, int Line, string Text)> ReadLines(string path)
    {
        var result = new List<(int Page, int Line, string Text)>();
        string[] pages = File.ReadAllText(path).Split('\f');

        for (int page = 0; page < pages.Length; page++)
        {
            // Nothing follows the last form feed.
            if (pages[page].Length == 0)
            {
                continue;
            }

            string[] pageLines = pages[page].Split("\r\n");
            int count = pageLines[^1].Length == 0 ? pageLines.Length - 1 : pageLines.Length;

            for (int line = 0; line < count; line++)
            {
                result.Add((page + 1, line + 1, pageLines[line]));
            }
        }

        return result;
    }
}
