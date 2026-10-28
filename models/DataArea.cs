namespace tuipos.Models;

// A named number kept in the database, like an IBM i *DEC data area.
public class DataArea
{
    // Up to 10 characters, like an IBM i object name, e.g. BARCODE.
    public string Name { get; set; } = "";

    public long Value { get; set; }
}
