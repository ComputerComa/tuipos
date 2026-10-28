using Terminal.Gui.App;
using Terminal.Gui.Input;
using tuipos.Reports;

namespace tuipos.UI;

// Reports option 5: list saved report output, with 4=Delete and 5=Display.
public class WorkWithSpooledFilesScreen
{
    private const int FirstRow = 7;
    private const int RowsPerPage = 12;

    private static readonly string Headings =
        $"{"File",-10}  {"Nbr",6}  {"User",-10}  {"Pages",5}  Date      Time";

    private readonly SpoolStore _spool;

    public WorkWithSpooledFilesScreen(SpoolStore spool)
    {
        _spool = spool;
    }

    public void Run(IApplication app)
    {
        using GreenScreenWindow window = new("F3=Exit   F5=Refresh   F12=Cancel");

        GreenScreen.AddHeader(window.Grid, "WRKSPLF", "Work with Spooled Files");

        var list = new OptionList<SpooledFile>(FirstRow, RowsPerPage, FormatFile, "(No spooled files)");

        void Load()
        {
            list.SetItems(_spool.List());
        }

        void Submit()
        {
            window.Message.Clear();

            var typed = list.TypedOptions();

            foreach (var (_, option, field) in typed)
            {
                if (option != "4" && option != "5")
                {
                    window.Message.Error(Messages.OptionNotValid(option));
                    field.SetFocus();
                    return;
                }
            }

            // Like a subfile program, handle each option in order down the page.
            foreach (var (file, option, _) in typed)
            {
                if (option == "5")
                {
                    new DisplaySpooledFileScreen(file).Run(app);
                }
            }

            var toDelete = typed.Where(t => t.Option == "4").Select(t => t.Item).ToList();

            if (toDelete.Count > 0 && ConfirmDelete(app, toDelete))
            {
                foreach (var file in toDelete)
                {
                    _spool.Delete(file);
                }

                window.Message.Show(Messages.SpooledFilesDeleted(toDelete.Count));
            }

            Load();
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
            GreenScreen.Text(3, 4, "4=Delete   5=Display", GreenScreen.FunctionKeys),
            GreenScreen.Text(1, 6, "Opt", GreenScreen.Bright),
            GreenScreen.Text(7, 6, Headings, GreenScreen.Bright)
        );

        list.AddTo(window.Grid);

        // Where the files are, so they can be copied or printed by hand.
        string folder = _spool.Folder.Length > 58 ? "..." + _spool.Folder[^55..] : _spool.Folder;

        window.Grid.Add(
            GreenScreen.Text(1, 20, GreenScreen.DotLeader("Saved in", 15) + " :"),
            GreenScreen.Text(20, 20, folder, GreenScreen.Bright)
        );

        Load();

        app.Run(window);
    }

    private bool ConfirmDelete(IApplication app, List<SpooledFile> files)
    {
        var rows = files.Select(FormatFile).ToList();
        return new ConfirmDeleteScreen("Confirm Delete of Spooled Files", Headings, rows).Run(app);
    }

    private static string FormatFile(SpooledFile file)
    {
        return $"{file.Name,-10}  {file.Number,6}  {GreenScreen.Clip(file.UserId, 10),-10}  "
            + $"{file.Pages,5}  {GreenScreen.Timestamp(file.Created)}";
    }
}
