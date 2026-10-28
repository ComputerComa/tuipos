namespace tuipos.UI;

// IBM-style message text. IDs are grouped by screen:
// TPS00xx sign-on, TPS01xx menus, TPS02xx register, TPS03xx payment,
// TPS04xx users, TPS05xx items, TPS06xx change password, TPS07xx reports,
// TPS08xx item inquiry and select, TPS09xx shared.
public static class Messages
{
    public const string UserIdRequired = "TPS0001 User must be entered.";

    public static string UserNotFound(string userId) =>
        $"TPS0002 User {userId} does not exist.";

    public static string PasswordNotCorrect(string userId) =>
        $"TPS0003 Password not correct for user profile {userId}.";

    public const string SelectionNotValid = "TPS0101 Selection or command not valid.";

    public static string NotAuthorized(string what) =>
        $"TPS0103 Not authorized to {what}.";

    public static string ItemNotFound(string barcode) =>
        $"TPS0201 Item {barcode} not found. Press F4 to search.";

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

    public static string UserCreated(string userId) =>
        $"TPS0401 User {userId} created.";

    public static string UsersChanged(int count) =>
        count == 1 ? "TPS0402 1 user changed." : $"TPS0402 {count} users changed.";

    public static string UsersDeleted(int count) =>
        count == 1 ? "TPS0403 1 user deleted." : $"TPS0403 {count} users deleted.";

    public static string CantDeleteSelf(string userId) =>
        $"TPS0404 Can't delete user {userId} while it's signed on.";

    public static string UserHasSales(string userId) =>
        $"TPS0405 User {userId} has sales and can't be deleted.";

    public const string LastAdmin =
        "TPS0406 At least one user must be an administrator.";

    public const string NewUserIdRequired = "TPS0410 User profile must be entered.";

    public static string UserExists(string userId) =>
        $"TPS0411 User {userId} already exists.";

    public const string FirstNameRequired = "TPS0412 First name must be entered.";

    public const string LastNameRequired = "TPS0413 Last name must be entered.";

    public const string PasswordNotValid = "TPS0414 Password must be 6 to 10 characters.";

    public const string AdminNotValid = "TPS0415 Administrator must be *YES or *NO.";

    public const string UserProfileNotValid =
        "TPS0416 User profile must start with a letter, $, # or @.";

    public static string ItemCreated(string barcode) =>
        $"TPS0501 Item {barcode} created.";

    public static string ItemsChanged(int count) =>
        count == 1 ? "TPS0502 1 item changed." : $"TPS0502 {count} items changed.";

    public static string ItemsDeleted(int count) =>
        count == 1 ? "TPS0503 1 item deleted." : $"TPS0503 {count} items deleted.";

    public const string BarcodeRequired = "TPS0510 Barcode must be entered.";

    public static string ItemExists(string barcode) =>
        $"TPS0511 Item {barcode} already exists.";

    public const string DescriptionRequired = "TPS0512 Description must be entered.";

    public const string PriceNotValid = "TPS0513 Type a price above zero, like 3.99.";

    public const string CurrentPasswordNotCorrect = "TPS0601 Current password not correct.";

    public const string NewPasswordNotValid = "TPS0602 New password must be 6 to 10 characters.";

    public const string NewPasswordsDontMatch = "TPS0603 New password entries don't match.";

    public const string NewPasswordSameAsCurrent =
        "TPS0604 New password must be different from the current password.";

    public const string PasswordChanged = "TPS0605 Password changed.";

    public static string SpooledFileCreated(int number, string name, int pages) =>
        $"TPS0701 Spooled file {number} {name} created, {pages} {(pages == 1 ? "page" : "pages")}.";

    public const string DateNotValid = "TPS0702 Type the date as MM/DD/YY.";

    public const string DateRangeNotValid = "TPS0703 From date can't be after the to date.";

    public static string SpooledFilesDeleted(int count) =>
        count == 1 ? "TPS0704 1 spooled file deleted." : $"TPS0704 {count} spooled files deleted.";

    public static string ItemsExported(int count, string path) =>
        $"TPS0705 {count} {(count == 1 ? "item" : "items")} exported to {path}.";

    public static string ExportFailed(string reason) =>
        GreenScreen.Clip($"TPS0706 Export failed: {reason}", GreenScreen.Columns - 2);

    public const string InquiryBarcodeRequired =
        "TPS0801 Type a barcode, or press F4 to search.";

    public static string InquiryItemNotFound(string barcode) =>
        $"TPS0802 Item {barcode} not found. Press F4 to search.";

    public const string SelectOneItem = "TPS0803 Select only one item.";

    public static string OptionNotValid(string option) =>
        $"TPS0901 Option {option} not valid.";
}
