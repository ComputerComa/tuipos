using Microsoft.EntityFrameworkCore;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

// SLTITM: the F4=Prompt list for a barcode field. Search by part of the
// description or the start of the barcode, then pick an item with 1=Select.
public class ItemSelectScreen
{
    private const int SearchLength = 30;
    private const int FirstRow = 9;
    private const int RowsPerPage = 12;
    private const string SelectOption = "1";

    private static readonly string Headings =
        $"{"Barcode",-13}  {"Description",-30}  {"Price",9}";

    private readonly PosDbContext _db;
    private readonly string _initialSearch;

    // initialSearch fills the search field, e.g. with what was typed in
    // the barcode field when F4 was pressed.
    public ItemSelectScreen(PosDbContext db, string initialSearch = "")
    {
        _db = db;
        _initialSearch = initialSearch.Trim();
    }

    // Returns the selected item, or null if the prompt was cancelled.
    public Item? Run(IApplication app)
    {
        Item? selected = null;

        using GreenScreenWindow window = new("F3=Exit   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "SLTITM", "Select Item");

        TextField searchField = GreenScreen.InputField(27, 4, SearchLength);
        searchField.Text = GreenScreen.Clip(_initialSearch, SearchLength);

        var list = new OptionList<Item>(FirstRow, RowsPerPage, FormatItem, "(No items match)");

        void Search()
        {
            string text = (searchField.Text ?? "").Trim();
            string lower = text.ToLowerInvariant();

            // Contains rather than LIKE, so % and _ in the search are plain text.
            var items = _db.Items
                .AsNoTracking()
                .Where(i => text.Length == 0
                    || i.Name.ToLower().Contains(lower)
                    || i.Barcode.StartsWith(text))
                .OrderBy(i => i.Name)
                .ToList();

            list.SetItems(items);

            // A new search starts on the first page.
            if (items.Count > 0)
            {
                list.ShowItem(items[0]);
            }
        }

        void Submit()
        {
            window.Message.Clear();

            // Like a subfile program, options are handled before the search.
            var typed = list.TypedOptions();

            foreach (var (_, option, field) in typed)
            {
                if (option != SelectOption)
                {
                    window.Message.Error(Messages.OptionNotValid(option));
                    field.SetFocus();
                    return;
                }
            }

            if (typed.Count > 1)
            {
                window.Message.Error(Messages.SelectOneItem);
                typed[1].Field.SetFocus();
                return;
            }

            if (typed.Count == 1)
            {
                selected = typed[0].Item;
                app.RequestStop();
                return;
            }

            Search();
        }

        searchField.Accepting += (_, e) =>
        {
            e.Handled = true;
            Submit();
        };

        list.OnEnter(Submit);

        window.KeyDown += (_, key) =>
        {
            if (key == Key.Enter)
            {
                // Enter with no field focused, e.g. an empty list.
                key.Handled = true;
                Submit();
            }
            else if (key == Key.F3 || key == Key.F12 || key == Key.Esc)
            {
                key.Handled = true;
                app.RequestStop();
            }
            else if (key == Key.PageDown)
            {
                key.Handled = true;
                list.PageDown();
            }
            else if (key == Key.PageUp)
            {
                key.Handled = true;
                list.PageUp();
            }
        };

        window.Grid.Add(
            GreenScreen.Text(1, 3, "Type part of the description or barcode, press Enter."),
            GreenScreen.Text(2, 4, GreenScreen.DotLeader("Search", 22)),
            searchField,
            GreenScreen.Text(1, 6, "Type option, press Enter."),
            GreenScreen.Text(3, 7, "1=Select", GreenScreen.FunctionKeys),
            GreenScreen.Text(1, 8, "Opt", GreenScreen.Bright),
            GreenScreen.Text(7, 8, Headings, GreenScreen.Bright)
        );

        // Added after the search field so Tab moves down the page.
        list.AddTo(window.Grid);

        Search();
        searchField.SetFocus();

        app.Run(window);

        return selected;
    }

    private static string FormatItem(Item item)
    {
        return $"{item.Barcode,-13}  {GreenScreen.Clip(item.Name, 30),-30}  "
            + $"{GreenScreen.Amount(item.PriceCents),9}";
    }
}
