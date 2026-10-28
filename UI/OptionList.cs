using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace tuipos.UI;

// A page of rows with a one-character option field beside each row, like
// an IBM subfile. Screens call PageUp and PageDown from their key handler
// and read TypedOptions when Enter is pressed.
public class OptionList<T>
{
    private readonly int _rowsPerPage;
    private readonly Func<T, string> _format;
    private readonly string _emptyText;

    private readonly TextField[] _optionFields;
    private readonly Label[] _rows;
    private readonly Label _more;

    private List<T> _items = new();
    private int _top;

    public OptionList(int firstRow, int rowsPerPage, Func<T, string> format, string emptyText)
    {
        _rowsPerPage = rowsPerPage;
        _format = format;
        _emptyText = emptyText;

        _optionFields = new TextField[rowsPerPage];
        _rows = new Label[rowsPerPage];

        for (int slot = 0; slot < rowsPerPage; slot++)
        {
            _optionFields[slot] = GreenScreen.InputField(2, firstRow + slot, 1, char.IsAsciiDigit);

            _rows[slot] = GreenScreen.Text(7, firstRow + slot, "");
            _rows[slot].Width = GreenScreen.Columns - 8;
        }

        // "More..." or "Bottom" sits right-aligned under the last row.
        _more = GreenScreen.Text(GreenScreen.Columns - 10, firstRow + rowsPerPage, "");
        _more.Width = 9;
    }

    public IReadOnlyList<T> Items => _items;

    // Add after any entry fields above the list so Tab moves down the page.
    public void AddTo(View grid)
    {
        grid.Add(_optionFields);
        grid.Add(_rows);
        grid.Add(_more);
    }

    // Replaces the rows, staying on the current page if it still exists.
    public void SetItems(IEnumerable<T> items)
    {
        _items = items.ToList();

        int lastPageTop = _items.Count == 0
            ? 0
            : (_items.Count - 1) / _rowsPerPage * _rowsPerPage;

        _top = Math.Min(_top, lastPageTop);
        Show();
    }

    // Moves to the page that holds item.
    public void ShowItem(T item)
    {
        int index = _items.IndexOf(item);

        if (index >= 0)
        {
            _top = index / _rowsPerPage * _rowsPerPage;
            Show();
        }
    }

    public void PageDown()
    {
        if (_top + _rowsPerPage < _items.Count)
        {
            _top += _rowsPerPage;
            Show();
        }
    }

    public void PageUp()
    {
        if (_top > 0)
        {
            _top -= _rowsPerPage;
            Show();
        }
    }

    // The options typed on this page, top to bottom.
    public List<(T Item, string Option, TextField Field)> TypedOptions()
    {
        var typed = new List<(T Item, string Option, TextField Field)>();

        for (int slot = 0; slot < _rowsPerPage && _top + slot < _items.Count; slot++)
        {
            string option = (_optionFields[slot].Text ?? "").Trim();

            if (option.Length > 0)
            {
                typed.Add((_items[_top + slot], option, _optionFields[slot]));
            }
        }

        return typed;
    }

    // Calls submit when Enter is pressed in any option field.
    public void OnEnter(Action submit)
    {
        foreach (var field in _optionFields)
        {
            field.Accepting += (_, e) =>
            {
                e.Handled = true;
                submit();
            };
        }
    }

    public void ClearOptions()
    {
        foreach (var field in _optionFields)
        {
            field.Text = "";
        }
    }

    private void Show()
    {
        for (int slot = 0; slot < _rowsPerPage; slot++)
        {
            int index = _top + slot;
            bool used = index < _items.Count;

            // Hidden fields drop out of the Tab order too.
            _optionFields[slot].Text = "";
            _optionFields[slot].Visible = used;
            _rows[slot].Text = used ? _format(_items[index]) : "";
        }

        if (_items.Count == 0)
        {
            _rows[0].Text = _emptyText;
        }

        string more = _top + _rowsPerPage < _items.Count ? "More..." : "Bottom";
        _more.Text = _items.Count == 0 ? "" : more.PadLeft(9);
    }
}
