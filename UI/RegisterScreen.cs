using Microsoft.EntityFrameworkCore;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

// Main menu option 1: ring up a sale.
public class RegisterScreen
{
    private const int BarcodeLength = 13;
    private const int QuantityLength = 3;
    private const int MaxQuantity = 999;
    private const string RemoveOption = "4";

    // The visible page of sale lines, like an IBM subfile page.
    private const int FirstLineRow = 11;
    private const int LinesPerPage = 8;

    private readonly PosDbContext _db;
    private readonly PosUser _user;

    public RegisterScreen(PosDbContext db, PosUser user)
    {
        _db = db;
        _user = user;
    }

    // Returns when the user exits back to the menu.
    public void Run(IApplication app)
    {
        var lines = new List<SaleLine>();
        int topLine = 0;

        // F3 with a sale in progress only exits on the second press.
        bool exitWarned = false;

        using GreenScreenWindow window = new("F3=Exit   F6=Pay   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "REGISTER", "Register");

        TextField barcodeField = GreenScreen.InputField(27, 5, BarcodeLength, char.IsAsciiDigit);
        TextField quantityField = GreenScreen.InputField(60, 5, QuantityLength, char.IsAsciiDigit);

        var optionFields = new TextField[LinesPerPage];
        var lineLabels = new Label[LinesPerPage];

        for (int slot = 0; slot < LinesPerPage; slot++)
        {
            optionFields[slot] = GreenScreen.InputField(2, FirstLineRow + slot, 1, char.IsAsciiDigit);

            lineLabels[slot] = GreenScreen.Text(7, FirstLineRow + slot, "");
            lineLabels[slot].Width = 72;
        }

        Label moreLabel = GreenScreen.Text(70, FirstLineRow + LinesPerPage, "");
        moreLabel.Width = 9;

        Label itemCountValue = GreenScreen.Text(69, 20, "", GreenScreen.Bright);
        itemCountValue.Width = 10;

        Label totalValue = GreenScreen.Text(69, 21, "", GreenScreen.Bright);
        totalValue.Width = 10;

        long Total() => lines.Sum(line => line.AmountCents);

        void ShowLines()
        {
            for (int slot = 0; slot < LinesPerPage; slot++)
            {
                int index = topLine + slot;
                bool used = index < lines.Count;

                optionFields[slot].Text = "";
                optionFields[slot].Visible = used;
                lineLabels[slot].Text = used ? FormatLine(lines[index]) : "";
            }

            if (lines.Count == 0)
            {
                lineLabels[0].Text = "(No items)";
            }

            string more = topLine + LinesPerPage < lines.Count ? "More..." : "Bottom";
            moreLabel.Text = lines.Count == 0 ? "" : more.PadLeft(9);

            itemCountValue.Text = lines.Sum(line => line.Quantity).ToString().PadLeft(10);
            totalValue.Text = GreenScreen.Amount(Total()).PadLeft(10);
        }

        void ClearEntry()
        {
            barcodeField.Text = "";
            quantityField.Text = "";

            foreach (var field in optionFields)
            {
                field.Text = "";
            }

            window.Message.Clear();
            barcodeField.SetFocus();
        }

        // Options typed next to lines are handled before a new item,
        // the same order a 5250 subfile program uses.
        bool ProcessOptions()
        {
            var removed = new List<SaleLine>();

            for (int slot = 0; slot < LinesPerPage; slot++)
            {
                string option = (optionFields[slot].Text ?? "").Trim();

                if (option.Length == 0)
                {
                    continue;
                }

                if (option != RemoveOption)
                {
                    window.Message.Error(Messages.LineOptionNotValid(option));
                    optionFields[slot].SetFocus();
                    return false;
                }

                removed.Add(lines[topLine + slot]);
            }

            if (removed.Count == 0)
            {
                return true;
            }

            lines.RemoveAll(removed.Contains);

            // Step back a page if the last one is now empty.
            if (topLine >= lines.Count && topLine > 0)
            {
                topLine -= LinesPerPage;
            }

            window.Message.Show(Messages.LinesRemoved(removed.Count));
            return true;
        }

        // Returns false when the entry has a problem; the cursor is then
        // already on the field to fix.
        bool AddItem()
        {
            string barcode = (barcodeField.Text ?? "").Trim();
            string quantityText = (quantityField.Text ?? "").Trim();

            if (barcode.Length == 0)
            {
                return true;
            }

            // A blank quantity means one.
            int quantity = quantityText.Length == 0 ? 1 : int.Parse(quantityText);

            var item = _db.Items
                .AsNoTracking()
                .FirstOrDefault(i => i.Barcode == barcode);

            if (item is null)
            {
                // Clear the barcode so the next scan isn't appended to it.
                barcodeField.Text = "";
                window.Message.Error(Messages.ItemNotFound(barcode));
                barcodeField.SetFocus();
                return false;
            }

            // Scanning an item that's already on the sale adds to its line.
            var line = lines.FirstOrDefault(l => l.Item.Id == item.Id);
            int newQuantity = (line?.Quantity ?? 0) + quantity;

            if (quantity < 1 || newQuantity > MaxQuantity)
            {
                window.Message.Error(Messages.QuantityNotValid);
                quantityField.SetFocus();
                return false;
            }

            if (line is null)
            {
                line = new SaleLine(item);
                lines.Add(line);
            }

            line.Quantity = newQuantity;

            barcodeField.Text = "";
            quantityField.Text = "";

            // Show the page holding the line that changed.
            topLine = lines.IndexOf(line) / LinesPerPage * LinesPerPage;
            return true;
        }

        void Submit()
        {
            exitWarned = false;
            window.Message.Clear();

            // A bad option stays on screen so it can be corrected.
            if (!ProcessOptions())
            {
                return;
            }

            bool added = AddItem();
            ShowLines();

            if (added)
            {
                barcodeField.SetFocus();
            }
        }

        void Pay()
        {
            if (lines.Count == 0)
            {
                window.Message.Error(Messages.NothingToPay);
                return;
            }

            long total = Total();
            var payment = new PaymentWindow(total).Run(app);

            if (payment is null)
            {
                barcodeField.SetFocus();
                return;
            }

            var sale = new Sale
            {
                TotalCents = total,
                PaymentMethod = payment.Method,
                UserId = _user.Id,
                Items = lines.Select(line => new SaleItem
                {
                    ItemId = line.Item.Id,
                    ItemName = line.Item.Name,
                    Barcode = line.Item.Barcode,
                    UnitPriceCents = line.Item.PriceCents,
                    Quantity = line.Quantity,
                    LineTotalCents = line.AmountCents
                }).ToList()
            };

            _db.Sales.Add(sale);
            _db.SaveChanges();

            lines.Clear();
            topLine = 0;
            ClearEntry();
            ShowLines();

            window.Message.Show(payment.ChangeCents > 0
                ? Messages.SaleCompleteWithChange(sale.Id, GreenScreen.Amount(payment.ChangeCents))
                : Messages.SaleComplete(sale.Id));
        }

        // Enter submits the screen from any field, including scanner input.
        barcodeField.Accepting += (_, e) =>
        {
            e.Handled = true;
            Submit();
        };

        quantityField.Accepting += (_, e) =>
        {
            e.Handled = true;
            Submit();
        };

        foreach (var field in optionFields)
        {
            field.Accepting += (_, e) =>
            {
                e.Handled = true;
                Submit();
            };
        }

        window.KeyDown += (_, key) =>
        {
            if (key == Key.F3)
            {
                key.Handled = true;

                if (lines.Count > 0 && !exitWarned)
                {
                    exitWarned = true;
                    window.Message.Error(Messages.SaleInProgress);
                    return;
                }

                app.RequestStop();
                return;
            }

            exitWarned = false;

            if (key == Key.F6)
            {
                key.Handled = true;
                Pay();
            }
            else if (key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                ClearEntry();
            }
            else if (key == Key.PageDown)
            {
                key.Handled = true;

                if (topLine + LinesPerPage < lines.Count)
                {
                    topLine += LinesPerPage;
                    ShowLines();
                }
            }
            else if (key == Key.PageUp)
            {
                key.Handled = true;

                if (topLine > 0)
                {
                    topLine -= LinesPerPage;
                    ShowLines();
                }
            }
        };

        string headings =
            $"{"Barcode",-13} {"Description",-30} {"Qty",4}  {"Price",9}  {"Amount",10}";

        window.Grid.Add(
            GreenScreen.Text(56, 2, "User:"),
            GreenScreen.Text(66, 2, _user.UserId, GreenScreen.Bright),
            GreenScreen.Text(1, 3, "Type barcode and quantity, press Enter."),
            GreenScreen.Text(2, 5, GreenScreen.DotLeader("Barcode", 22)),
            barcodeField,
            GreenScreen.Text(45, 5, GreenScreen.DotLeader("Quantity", 12)),
            quantityField,
            GreenScreen.Text(1, 7, "Type options, press Enter."),
            GreenScreen.Text(3, 8, "4=Remove", GreenScreen.FunctionKeys),
            GreenScreen.Text(1, 10, "Opt", GreenScreen.Bright),
            GreenScreen.Text(7, 10, headings, GreenScreen.Bright)
        );

        // Option fields go after the entry fields so Tab moves down the page.
        window.Grid.Add(optionFields);
        window.Grid.Add(lineLabels);

        window.Grid.Add(
            moreLabel,
            GreenScreen.Text(46, 20, GreenScreen.DotLeader("Items", 19) + " :"),
            itemCountValue,
            GreenScreen.Text(46, 21, GreenScreen.DotLeader("Total", 19) + " :"),
            totalValue
        );

        ShowLines();
        barcodeField.SetFocus();

        window.RunWithClock(app);
    }

    // One sale line, e.g. "100000000001  Whole Milk - 1 gal   2   3.99   7.98".
    private static string FormatLine(SaleLine line)
    {
        string name = line.Item.Name.Length > 30 ? line.Item.Name[..30] : line.Item.Name;

        return $"{line.Item.Barcode,-13} {name,-30} {line.Quantity,4}  "
            + $"{GreenScreen.Amount(line.Item.PriceCents),9}  "
            + $"{GreenScreen.Amount(line.AmountCents),10}";
    }

    private class SaleLine
    {
        public SaleLine(Item item)
        {
            Item = item;
        }

        public Item Item { get; }
        public int Quantity { get; set; }
        public long AmountCents => Item.PriceCents * Quantity;
    }
}
