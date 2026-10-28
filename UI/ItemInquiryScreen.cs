using Microsoft.EntityFrameworkCore;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

// Main menu option 2: a price check. Scan or type a barcode, or press F4
// to search, and see the item's description, price and recent sales.
public class ItemInquiryScreen
{
    private const int BarcodeLength = 13;

    private const int PromptX = 2;
    private const int PromptWidth = 22;
    private const int FieldX = 27;
    private const int FirstOutputRow = 7;

    private static readonly string[] Prompts =
    {
        "Barcode",
        "Description",
        "Price",
        "Sold today",
        "Last sold"
    };

    private readonly PosDbContext _db;

    public ItemInquiryScreen(PosDbContext db)
    {
        _db = db;
    }

    public void Run(IApplication app)
    {
        using GreenScreenWindow window = new("F3=Exit   F4=Prompt   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "ITMINQ", "Item Inquiry");

        TextField barcodeField = GreenScreen.InputField(FieldX, 5, BarcodeLength, char.IsAsciiDigit);

        var values = new Label[Prompts.Length];

        for (int i = 0; i < Prompts.Length; i++)
        {
            values[i] = GreenScreen.Text(FieldX, FirstOutputRow + i, "", GreenScreen.Bright);
            values[i].Width = GreenScreen.Columns - FieldX - 1;
        }

        void ShowItem(Item? item)
        {
            string[] text = item is null ? new string[Prompts.Length] : ItemValues(item);

            for (int i = 0; i < Prompts.Length; i++)
            {
                values[i].Text = text[i] ?? "";
            }
        }

        void Inquire()
        {
            window.Message.Clear();

            string barcode = (barcodeField.Text ?? "").Trim();

            // Clear the field so the next scan isn't appended to this one.
            barcodeField.Text = "";

            if (barcode.Length == 0)
            {
                window.Message.Error(Messages.InquiryBarcodeRequired);
                return;
            }

            var item = _db.Items
                .AsNoTracking()
                .FirstOrDefault(i => i.Barcode == barcode);

            ShowItem(item);

            if (item is null)
            {
                window.Message.Error(Messages.InquiryItemNotFound(barcode));
            }
        }

        void Prompt()
        {
            window.Message.Clear();

            var item = new ItemSelectScreen(_db, barcodeField.Text ?? "").Run(app);

            if (item is not null)
            {
                barcodeField.Text = "";
                ShowItem(item);
            }

            barcodeField.SetFocus();
        }

        barcodeField.Accepting += (_, e) =>
        {
            e.Handled = true;
            Inquire();
        };

        window.KeyDown += (_, key) =>
        {
            if (key == Key.F3 || key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                app.RequestStop();
            }
            else if (key == Key.F4)
            {
                key.Handled = true;
                Prompt();
            }
        };

        window.Grid.Add(
            GreenScreen.Text(1, 3, "Type barcode, press Enter."),
            GreenScreen.Text(PromptX, 5, GreenScreen.DotLeader("Barcode", PromptWidth)),
            barcodeField
        );

        for (int i = 0; i < Prompts.Length; i++)
        {
            window.Grid.Add(
                GreenScreen.Text(PromptX, FirstOutputRow + i, GreenScreen.DotLeader(Prompts[i], PromptWidth - 2) + " :"),
                values[i]
            );
        }

        barcodeField.SetFocus();

        window.RunWithClock(app);
    }

    // In the same order as Prompts.
    private string[] ItemValues(Item item)
    {
        // SQLite can't compare DateTimeOffset values in a query, so the
        // dates are worked out here instead of in the database.
        var sales = _db.SaleItems
            .AsNoTracking()
            .Where(si => si.ItemId == item.Id)
            .Select(si => new { si.Quantity, si.Sale.CreatedAt })
            .AsEnumerable()
            .Select(s => (s.Quantity, Local: s.CreatedAt.LocalDateTime))
            .ToList();

        int soldToday = sales
            .Where(s => s.Local.Date == DateTime.Today)
            .Sum(s => s.Quantity);

        string lastSold = sales.Count == 0
            ? "*NONE"
            : GreenScreen.Timestamp(sales.Max(s => s.Local));

        return new[]
        {
            item.Barcode,
            item.Name,
            GreenScreen.Amount(item.PriceCents),
            soldToday.ToString(),
            lastSold
        };
    }
}
