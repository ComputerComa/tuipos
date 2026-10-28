namespace tuipos.Models;

public class PosUser
{
    public int Id { get; set; }

    // The user profile name entered at sign-on, like QSECOFR.
    public string UserId { get; set; } = "";

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    // The password hash. The column kept its name from when sign-on used PINs.
    public string PinHash { get; set; } = "";

    // Administrators can open the Administration menu.
    public bool IsAdmin { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
