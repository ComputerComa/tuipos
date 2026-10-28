namespace tuipos.UI;

// IBM-style message text. IDs are grouped by screen:
// TPS00xx sign-on, TPS01xx main menu.
public static class Messages
{
    public const string UserIdRequired = "TPS0001 User ID must be entered.";

    public static string UserNotFound(string userId) =>
        $"TPS0002 User {userId} does not exist.";

    public static string PinNotCorrect(string userId) =>
        $"TPS0003 PIN not correct for user {userId}.";

    public const string SelectionNotValid = "TPS0101 Selection or command not valid.";

    public static string OptionNotAvailable(int option) =>
        $"TPS0102 Option {option} is not available yet.";
}
