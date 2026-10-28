using System.Globalization;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Reports;

namespace tuipos.UI;

// Asks for a report's date, or date range, before it runs. Both default to
// today, like an IBM command prompt filled in with defaults.
public class ReportPromptScreen
{
    private const int DateLength = 10;

    private const int PromptX = 2;
    private const int PromptWidth = 28;
    private const int FieldX = 33;
    private const int HintX = 46;

    private static readonly string[] DateFormats = { "MM/dd/yy", "M/d/yy", "MM/dd/yyyy", "M/d/yyyy" };

    private readonly string _reportId;
    private readonly string _title;
    private readonly bool _dateRange;

    public ReportPromptScreen(string reportId, string title, bool dateRange)
    {
        _reportId = reportId;
        _title = title;
        _dateRange = dateRange;
    }

    // Returns the chosen dates, or null if cancelled. For a single date,
    // From and To are the same.
    public (DateOnly From, DateOnly To)? Run(IApplication app)
    {
        (DateOnly From, DateOnly To)? result = null;

        using GreenScreenWindow window = new("F3=Exit   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, _reportId, _title);

        string today = SalesData.DateText(DateOnly.FromDateTime(DateTime.Today));

        TextField fromField = GreenScreen.InputField(FieldX, 5, DateLength, c => char.IsAsciiDigit(c) || c == '/');
        TextField toField = GreenScreen.InputField(FieldX, 6, DateLength, c => char.IsAsciiDigit(c) || c == '/');

        fromField.Text = today;
        toField.Text = today;

        void Fail(TextField field, string message)
        {
            window.Message.Error(message);
            field.SetFocus();
        }

        void Submit()
        {
            window.Message.Clear();

            if (ParseDate(fromField.Text) is not { } from)
            {
                Fail(fromField, Messages.DateNotValid);
                return;
            }

            var to = from;

            if (_dateRange)
            {
                if (ParseDate(toField.Text) is not { } parsedTo)
                {
                    Fail(toField, Messages.DateNotValid);
                    return;
                }

                to = parsedTo;

                if (from > to)
                {
                    Fail(fromField, Messages.DateRangeNotValid);
                    return;
                }
            }

            result = (from, to);
            app.RequestStop();
        }

        foreach (var field in new[] { fromField, toField })
        {
            field.Accepting += (_, e) =>
            {
                e.Handled = true;
                Submit();
            };
        }

        window.KeyDown += (_, key) =>
        {
            if (key == Key.F3 || key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                app.RequestStop();
            }
        };

        window.Grid.Add(
            GreenScreen.Text(1, 3, "Type choices, press Enter."),
            GreenScreen.Text(
                PromptX,
                5,
                GreenScreen.DotLeader(_dateRange ? "From date" : "Report date", PromptWidth)),
            fromField,
            GreenScreen.Text(HintX, 5, "MM/DD/YY", GreenScreen.FunctionKeys)
        );

        if (_dateRange)
        {
            window.Grid.Add(
                GreenScreen.Text(PromptX, 6, GreenScreen.DotLeader("To date", PromptWidth)),
                toField,
                GreenScreen.Text(HintX, 6, "MM/DD/YY", GreenScreen.FunctionKeys)
            );
        }

        app.Run(window);

        return result;
    }

    private static DateOnly? ParseDate(string? text)
    {
        return DateOnly.TryParseExact(
            (text ?? "").Trim(),
            DateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }
}
