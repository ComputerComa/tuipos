using System.Text;
using tuipos.UI;

namespace tuipos.Reports;

// A finished report, ready to save as a spooled file.
public record Report(string Id, string Text, int Pages);

// Lays out a report the way an IBM printer file does: 132 columns, 66-line
// pages with overflow at line 60, a heading on every page, and a form feed
// after each page so a dot matrix printer ejects to the next sheet. Lines
// end in CRLF so the file can be sent to the printer as-is.
public class ReportWriter
{
    public const int Width = 132;

    // Like PAGESIZE(66 132) OVRFLW(60) on an IBM printer file.
    private const int OverflowLine = 60;

    private readonly string _reportId;
    private readonly string _title;
    private readonly string _userId;
    private readonly DateTime _created = DateTime.Now;

    private readonly List<string> _subheadings = new();
    private readonly List<string> _columnHeadings = new();
    private readonly StringBuilder _text = new();

    private int _page;
    private int _lineOnPage;

    public ReportWriter(string reportId, string title, string userId)
    {
        _reportId = reportId;
        _title = title;
        _userId = userId;
    }

    // Printed under the page heading on every page, e.g. the date range.
    public void Subheading(string text)
    {
        _subheadings.Add(text);
    }

    // Printed at the top of every page, above the detail lines.
    public void ColumnHeadings(params string[] lines)
    {
        _columnHeadings.AddRange(lines);
    }

    public void Line(string text = "")
    {
        if (_page == 0 || _lineOnPage >= OverflowLine)
        {
            StartPage();
        }

        Write(text);
    }

    // Ends with IBM's end-of-listing line and the final form feed.
    public Report Finish()
    {
        Line();
        Line(Compose("", "* * * * *   E N D   O F   L I S T I N G   * * * * *", ""));

        _text.Append('\f');

        return new Report(_reportId, _text.ToString(), _page);
    }

    // Places text at the left, center and right of a 132-column line.
    public static string Compose(string left, string center, string right)
    {
        var line = new char[Width];
        Array.Fill(line, ' ');

        Put(line, 0, left);
        Put(line, (Width - center.Length) / 2, center);
        Put(line, Width - right.Length, right);

        return new string(line).TrimEnd();
    }

    private void StartPage()
    {
        if (_page > 0)
        {
            _text.Append('\f');
        }

        _page++;
        _lineOnPage = 0;

        Write(Compose(_reportId, _title, $"Page {_page,4}"));
        Write(Compose(GreenScreen.SystemName, "", $"User {_userId}   {GreenScreen.Timestamp(_created)}"));

        foreach (var subheading in _subheadings)
        {
            Write(subheading);
        }

        Write("");

        foreach (var heading in _columnHeadings)
        {
            Write(heading);
        }
    }

    private void Write(string text)
    {
        _text.Append(GreenScreen.Clip(text.TrimEnd(), Width)).Append("\r\n");
        _lineOnPage++;
    }

    private static void Put(char[] line, int start, string text)
    {
        for (int i = 0; i < text.Length && start + i < line.Length; i++)
        {
            if (start + i >= 0)
            {
                line[start + i] = text[i];
            }
        }
    }
}
