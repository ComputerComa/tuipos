namespace tuipos.Models;

public class PosUser
{
    public int Id { get; set; }

    // The user-facing ID entered at login.
    public string UserId { get; set; } = "";

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    // Store a password hash here, never the six-digit PIN itself.
    public string PinHash { get; set; } = "";

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
