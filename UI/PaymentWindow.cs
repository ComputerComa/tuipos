using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using tuipos.Models;

namespace tuipos.UI;

public record PaymentResult(PaymentMethod Method, long ChangeCents);

// A 5250-style pop-up window over the register for taking payment.
public class PaymentWindow
{
    private const int Width = 56;
    private const int Height = 14;

    // Input prompts end where output prompts put their colon, so
    // fields and values line up at FieldX.
    private const int PromptX = 3;
    private const int PromptWidth = 22;
    private const int FieldX = 28;

    private const int TenderedLength = 8;

    private readonly long _totalCents;

    public PaymentWindow(long totalCents)
    {
        _totalCents = totalCents;
    }

    // Returns the payment, or null if it was cancelled.
    public PaymentResult? Run(IApplication app)
    {
        PaymentResult? result = null;

        using Window window = new()
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Width,
            Height = Height,
            BorderStyle = LineStyle.None
        };

        window.SetScheme(GreenScreen.Normal);

        Label title = GreenScreen.Text(0, 1, "Payment", GreenScreen.Bright);
        title.X = Pos.Center();

        TextField methodField = GreenScreen.InputField(FieldX, 5, 1, char.IsAsciiDigit);
        TextField tenderedField = GreenScreen.InputField(
            FieldX,
            9,
            TenderedLength,
            c => char.IsAsciiDigit(c) || c == '.'
        );

        MessageLine message = new()
        {
            X = PromptX,
            Y = 11,
            Width = Width - PromptX * 2
        };

        void TryPay()
        {
            message.Clear();

            PaymentMethod? method = (methodField.Text ?? "").Trim() switch
            {
                "1" => PaymentMethod.Cash,
                "2" => PaymentMethod.Card,
                "3" => PaymentMethod.Other,
                _ => null
            };

            if (method is null)
            {
                message.Error(Messages.PaymentMethodNotValid);
                methodField.SetFocus();
                return;
            }

            if (method != PaymentMethod.Cash)
            {
                result = new PaymentResult(method.Value, 0);
                app.RequestStop();
                return;
            }

            if (!GreenScreen.TryParseAmount((tenderedField.Text ?? "").Trim(), out long tendered))
            {
                message.Error(Messages.AmountNotValid);
                tenderedField.SetFocus();
                return;
            }

            if (tendered < _totalCents)
            {
                message.Error(Messages.AmountTooLow(GreenScreen.Amount(_totalCents)));
                tenderedField.SetFocus();
                return;
            }

            result = new PaymentResult(PaymentMethod.Cash, tendered - _totalCents);
            app.RequestStop();
        }

        methodField.Accepting += (_, e) =>
        {
            e.Handled = true;

            // Cash still needs the amount tendered, so move there first.
            if ((methodField.Text ?? "").Trim() == "1"
                && (tenderedField.Text ?? "").Trim().Length == 0)
            {
                message.Clear();
                tenderedField.SetFocus();
                return;
            }

            TryPay();
        };

        tenderedField.Accepting += (_, e) =>
        {
            e.Handled = true;
            TryPay();
        };

        window.KeyDown += (_, key) =>
        {
            if (key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                app.RequestStop();
            }
        };

        // Added first so everything else draws on top of the border rows,
        // which also blank out the register underneath.
        window.Add(Border());

        window.Add(
            title,
            GreenScreen.OutputLine(
                PromptX,
                3,
                "Total due",
                PromptWidth - 2,
                GreenScreen.Amount(_totalCents)
            ),
            GreenScreen.Text(PromptX, 5, GreenScreen.DotLeader("Payment method", PromptWidth)),
            methodField,
            GreenScreen.Text(33, 5, "1=Cash", GreenScreen.FunctionKeys),
            GreenScreen.Text(33, 6, "2=Card", GreenScreen.FunctionKeys),
            GreenScreen.Text(33, 7, "3=Other", GreenScreen.FunctionKeys),
            GreenScreen.Text(PromptX, 9, GreenScreen.DotLeader("Amount tendered", PromptWidth)),
            tenderedField,
            GreenScreen.Text(FieldX + TenderedLength + 3, 9, "(cash)"),
            message,
            GreenScreen.Text(PromptX, 12, "F12=Cancel", GreenScreen.FunctionKeys)
        );

        app.Run(window);

        return result;
    }

    // IBM i draws window borders with dots along the top and bottom
    // and colons down the sides.
    private static View[] Border()
    {
        string inside = new(' ', Width - 2);

        var rows = new View[Height];
        rows[0] = GreenScreen.Text(0, 0, new string('.', Width));

        for (int y = 1; y < Height - 1; y++)
        {
            rows[y] = GreenScreen.Text(0, y, ":" + inside + ":");
        }

        rows[Height - 1] = GreenScreen.Text(0, Height - 1, ":" + new string('.', Width - 2) + ":");

        return rows;
    }
}
