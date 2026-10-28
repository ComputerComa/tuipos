namespace tuipos.UI;

// IBM-style message text. IDs are grouped by screen:
// TPS00xx sign-on, TPS01xx main menu, TPS02xx register, TPS03xx payment.
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

    public static string ItemNotFound(string barcode) =>
        $"TPS0201 Item {barcode} not found.";

    public const string QuantityNotValid = "TPS0202 Quantity must be 1 to 999.";

    public static string LineOptionNotValid(string option) =>
        $"TPS0203 Option {option} not valid. Type 4 to remove a line.";

    public static string LinesRemoved(int count) =>
        count == 1 ? "TPS0204 1 line removed." : $"TPS0204 {count} lines removed.";

    public const string NothingToPay = "TPS0205 Add an item before paying.";

    public const string SaleInProgress =
        "TPS0206 Sale in progress. Press F3 again to discard it.";

    public static string SaleComplete(int saleId) =>
        $"TPS0207 Sale {saleId} complete.";

    public static string SaleCompleteWithChange(int saleId, string change) =>
        $"TPS0208 Sale {saleId} complete. Change due {change}.";

    public const string PaymentMethodNotValid =
        "TPS0301 Payment method must be 1, 2 or 3.";

    public const string AmountNotValid =
        "TPS0302 Type the amount tendered, like 20.00.";

    public static string AmountTooLow(string total) =>
        $"TPS0303 Amount tendered must be at least {total}.";
}
