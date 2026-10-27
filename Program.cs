using Microsoft.EntityFrameworkCore;
using tuipos.Data;

using var db = new PosDbContext();

// The database must have the current schema before seeding.
await db.Database.MigrateAsync();

await DbSeeder.SeedAsync(db);

Console.WriteLine("Database seeded.");

var itemCount = await db.Items.CountAsync();
var userCount = await db.Users.CountAsync();

Console.WriteLine($"Users: {userCount}");
Console.WriteLine($"Items: {itemCount}");
