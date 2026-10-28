using Microsoft.EntityFrameworkCore;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using tuipos.Data;
using tuipos.Models;

namespace tuipos.UI;

// Administration option 2: list items, with 2=Change, 4=Delete and F6=Create.
public class WorkWithItemsScreen
{
    private const int FirstRow = 7;
    private const int RowsPerPage = 12;

    private static readonly string Headings =
        $"{"Barcode",-13}  {"Description",-30}  {"Price",9}";

    private readonly PosDbContext _db;

    public WorkWithItemsScreen(PosDbContext db)
    {
        _db = db;
    }

    public void Run(IApplication app)
    {
        using GreenScreenWindow window = new("F3=Exit   F5=Refresh   F6=Create   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "WRKITM", "Work with Items");

        var list = new OptionList<Item>(FirstRow, RowsPerPage, FormatItem, "(No items)");

        void Load()
        {
            list.SetItems(_db.Items.AsNoTracking().OrderBy(i => i.Barcode).ToList());
        }

        void Submit()
        {
            window.Message.Clear();

            var typed = list.TypedOptions();

            foreach (var (_, option, field) in typed)
            {
                if (option != "2" && option != "4")
                {
                    window.Message.Error(Messages.OptionNotValid(option));
                    field.SetFocus();
                    return;
                }
            }

            string? info = null;

            // Like a subfile program, handle each option in order down the page.
            int changed = 0;

            foreach (var (item, option, _) in typed)
            {
                if (option == "2" && new ItemEditScreen(_db, item).Run(app))
                {
                    changed++;
                }
            }

            if (changed > 0)
            {
                info = Messages.ItemsChanged(changed);
            }

            var toDelete = typed.Where(t => t.Option == "4").Select(t => t.Item).ToList();

            if (toDelete.Count > 0 && ConfirmDelete(app, toDelete))
            {
                // Past sales keep their own copy of the name and price, and
                // the database clears their link to the deleted item.
                var ids = toDelete.Select(i => i.Id).ToList();
                _db.Items.Where(i => ids.Contains(i.Id)).ExecuteDelete();

                info = Messages.ItemsDeleted(toDelete.Count);
            }

            Load();

            if (info is not null)
            {
                window.Message.Show(info);
            }
        }

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
            else if (key == Key.F5)
            {
                key.Handled = true;
                window.Message.Clear();
                Load();
            }
            else if (key == Key.F6)
            {
                key.Handled = true;
                var editor = new ItemEditScreen(_db, null);

                if (editor.Run(app))
                {
                    Load();
                    window.Message.Show(Messages.ItemCreated(editor.SavedBarcode));
                }
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
            GreenScreen.Text(1, 3, "Type options, press Enter."),
            GreenScreen.Text(3, 4, "2=Change   4=Delete", GreenScreen.FunctionKeys),
            GreenScreen.Text(1, 6, "Opt", GreenScreen.Bright),
            GreenScreen.Text(7, 6, Headings, GreenScreen.Bright)
        );

        list.AddTo(window.Grid);
        Load();

        app.Run(window);
    }

    private bool ConfirmDelete(IApplication app, List<Item> items)
    {
        var rows = items.Select(FormatItem).ToList();
        return new ConfirmDeleteScreen("Confirm Delete of Items", Headings, rows).Run(app);
    }

    private static string FormatItem(Item item)
    {
        return $"{item.Barcode,-13}  {GreenScreen.Clip(item.Name, 30),-30}  "
            + $"{GreenScreen.Amount(item.PriceCents),9}";
    }
}
