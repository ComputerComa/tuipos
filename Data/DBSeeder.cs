using Microsoft.EntityFrameworkCore;
using tuipos.Models;

namespace tuipos.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(PosDbContext db)
    {
        await SeedUserAsync(db);
        await EnsureAdminAsync(db);
        await SeedItemsAsync(db);

        await db.SaveChangesAsync();

        // The seeded items must be saved first, so the data area starts
        // after their barcodes.
        StoreBarcodes.EnsureDataArea(db);
        await db.SaveChangesAsync();

        // The context lives as long as the app, so don't keep the seeded
        // rows tracked.
        db.ChangeTracker.Clear();
    }

    // Only seeds an empty table, so users deleted in WRKUSR stay deleted.
    private static async Task SeedUserAsync(PosDbContext db)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        // IBM i ships the security officer with its name as its password.
        var user = new PosUser
        {
            UserId = "QSECOFR",
            FirstName = "Security",
            LastName = "Officer",
            IsAdmin = true
        };

        user.PinHash = Passwords.Hash(user, "QSECOFR");

        db.Users.Add(user);
    }

    // Databases from before IsAdmin existed have no admins, which would
    // lock everyone out of Administration. Promote the first user.
    private static async Task EnsureAdminAsync(PosDbContext db)
    {
        if (await db.Users.AnyAsync(user => user.IsAdmin))
        {
            return;
        }

        var firstUser = await db.Users
            .OrderBy(user => user.Id)
            .FirstOrDefaultAsync();

        if (firstUser is not null)
        {
            firstUser.IsAdmin = true;
        }
    }

    // Only seeds an empty table, so items deleted in WRKITM stay deleted.
    private static async Task SeedItemsAsync(PosDbContext db)
    {
        if (await db.Items.AnyAsync())
        {
            return;
        }

        var items = new List<Item>
        {
            new() { Barcode = "100000000001", Name = "Whole Milk - 1 gal",       PriceCents = 399 },
            new() { Barcode = "100000000002", Name = "White Bread",               PriceCents = 249 },
            new() { Barcode = "100000000003", Name = "Large Eggs - 12 ct",        PriceCents = 429 },
            new() { Barcode = "100000000004", Name = "Butter - 1 lb",             PriceCents = 499 },
            new() { Barcode = "100000000005", Name = "Ground Coffee - 12 oz",    PriceCents = 899 },
            new() { Barcode = "100000000006", Name = "Orange Juice - 52 oz",     PriceCents = 479 },
            new() { Barcode = "100000000007", Name = "Bottled Water - 20 oz",    PriceCents = 149 },
            new() { Barcode = "100000000008", Name = "Cola - 20 oz",             PriceCents = 229 },
            new() { Barcode = "100000000009", Name = "Potato Chips - 8 oz",      PriceCents = 399 },
            new() { Barcode = "100000000010", Name = "Chocolate Bar",            PriceCents = 189 },
            new() { Barcode = "100000000011", Name = "Peanut Butter - 16 oz",    PriceCents = 349 },
            new() { Barcode = "100000000012", Name = "Strawberry Jelly - 18 oz", PriceCents = 329 },
            new() { Barcode = "100000000013", Name = "Pasta - 16 oz",            PriceCents = 179 },
            new() { Barcode = "100000000014", Name = "Pasta Sauce - 24 oz",      PriceCents = 299 },
            new() { Barcode = "100000000015", Name = "Rice - 2 lb",              PriceCents = 299 },
            new() { Barcode = "100000000016", Name = "Canned Soup",              PriceCents = 199 },
            new() { Barcode = "100000000017", Name = "Paper Towels - 2 rolls",   PriceCents = 499 },
            new() { Barcode = "100000000018", Name = "Dish Soap - 18 oz",        PriceCents = 349 },
            new() { Barcode = "100000000019", Name = "AA Batteries - 4 ct",      PriceCents = 649 },
            new() { Barcode = "100000000020", Name = "Notebook",                  PriceCents = 199 }
        };

        db.Items.AddRange(items);
    }
}
