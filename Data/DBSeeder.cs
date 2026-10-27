using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tuipos.Models;

namespace tuipos.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(PosDbContext db)
    {
        await SeedUserAsync(db);
        await SeedItemsAsync(db);

        await db.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(PosDbContext db)
    {
        const string userId = "0000";

        var userExists = await db.Users
            .AnyAsync(user => user.UserId == userId);

        if (userExists)
        {
            return;
        }

        var user = new PosUser
        {
            UserId = userId,
            FirstName = "Local",
            LastName = "Admin",
        };

        var hasher = new PasswordHasher<PosUser>();
        user.PinHash = hasher.HashPassword(user, "123456");

        db.Users.Add(user);
    }

    private static async Task SeedItemsAsync(PosDbContext db)
    {
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

        var barcodes = items.Select(item => item.Barcode).ToArray();

        var existingBarcodes = await db.Items
            .Where(item => barcodes.Contains(item.Barcode))
            .Select(item => item.Barcode)
            .ToListAsync();

        var existing = existingBarcodes.ToHashSet();

        foreach (var item in items)
        {
            if (!existing.Contains(item.Barcode))
            {
                db.Items.Add(item);
            }
        }
    }
}
