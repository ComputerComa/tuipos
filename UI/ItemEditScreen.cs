using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

// CRTITM and CHGITM: create an item, or change one picked on WRKITM.
public class ItemEditScreen
{
    private const int BarcodeLength = 13;
    private const int DescriptionLength = 30;
    private const int PriceLength = 8;

    private const int PromptX = 2;
    private const int PromptWidth = 28;
    private const int FieldX = 33;

    private readonly PosDbContext _db;
    private readonly Item? _existing;

    // Pass null to create a new item.
    public ItemEditScreen(PosDbContext db, Item? existing)
    {
        _db = db;
        _existing = existing;
    }

    public string SavedBarcode { get; private set; } = "";

    // Returns true if the item was saved.
    public bool Run(IApplication app)
    {
        bool saved = false;
        bool creating = _existing is null;

        using GreenScreenWindow window = new(creating
            ? "F3=Exit   F6=Next barcode   F12=Cancel"
            : "F3=Exit   F12=Cancel");

        GreenScreen.AddHeader(
            window.Grid,
            creating ? "CRTITM" : "CHGITM",
            creating ? "Create Item" : "Change Item"
        );

        TextField barcodeField = GreenScreen.InputField(FieldX, 5, BarcodeLength, char.IsAsciiDigit);
        TextField descriptionField = GreenScreen.InputField(FieldX, 6, DescriptionLength);
        TextField priceField = GreenScreen.InputField(
            FieldX,
            7,
            PriceLength,
            c => char.IsAsciiDigit(c) || c == '.'
        );

        descriptionField.Text = _existing?.Name ?? "";
        priceField.Text = _existing is null ? "" : GreenScreen.Amount(_existing.PriceCents);

        void Fail(TextField field, string message)
        {
            window.Message.Error(message);
            field.SetFocus();
        }

        void Save()
        {
            window.Message.Clear();

            string barcode = _existing?.Barcode ?? (barcodeField.Text ?? "").Trim();
            string description = (descriptionField.Text ?? "").Trim();

            if (creating && barcode.Length == 0)
            {
                Fail(barcodeField, Messages.BarcodeRequired);
                return;
            }

            if (creating && _db.Items.Any(i => i.Barcode == barcode))
            {
                Fail(barcodeField, Messages.ItemExists(barcode));
                return;
            }

            if (description.Length == 0)
            {
                Fail(descriptionField, Messages.DescriptionRequired);
                return;
            }

            if (!GreenScreen.TryParseAmount((priceField.Text ?? "").Trim(), out long priceCents)
                || priceCents <= 0)
            {
                Fail(priceField, Messages.PriceNotValid);
                return;
            }

            var item = _existing is null
                ? new Item { Barcode = barcode }
                : _db.Items.First(i => i.Id == _existing.Id);

            item.Name = description;
            item.PriceCents = priceCents;

            if (creating)
            {
                _db.Items.Add(item);
                StoreBarcodes.Record(_db, barcode);
            }

            _db.SaveChanges();

            // The context lives as long as the app, so don't keep entities
            // tracked between screens.
            _db.ChangeTracker.Clear();

            SavedBarcode = barcode;
            saved = true;
            app.RequestStop();
        }

        foreach (var field in new[] { barcodeField, descriptionField, priceField })
        {
            field.Accepting += (_, e) =>
            {
                e.Handled = true;
                Save();
            };
        }

        window.KeyDown += (_, key) =>
        {
            if (key == Key.F3 || key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                app.RequestStop();
            }
            else if (key == Key.F6 && creating)
            {
                key.Handled = true;
                window.Message.Clear();
                barcodeField.Text = StoreBarcodes.Next(_db);
                descriptionField.SetFocus();
            }
        };

        window.Grid.Add(
            GreenScreen.Text(1, 3, "Type choices, press Enter."),
            GreenScreen.Text(PromptX, 5, GreenScreen.DotLeader("Barcode", PromptWidth))
        );

        // The barcode is the key, so Change shows it but doesn't let it change.
        if (creating)
        {
            window.Grid.Add(barcodeField);
        }
        else
        {
            window.Grid.Add(GreenScreen.Text(FieldX, 5, _existing!.Barcode, GreenScreen.Bright));
        }

        window.Grid.Add(
            GreenScreen.Text(PromptX, 6, GreenScreen.DotLeader("Description", PromptWidth)),
            descriptionField,
            GreenScreen.Text(PromptX, 7, GreenScreen.DotLeader("Price", PromptWidth)),
            priceField
        );

        app.Run(window);

        return saved;
    }
}
