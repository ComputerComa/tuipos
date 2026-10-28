using Microsoft.EntityFrameworkCore;
using Terminal.Gui.App;
using tuipos.Data;
using tuipos.UI;

using var db = new PosDbContext();

await db.Database.MigrateAsync();
await DbSeeder.SeedAsync(db);

using IApplication app = Application.Create();
app.Init();

while (true)
{
    var signOnScreen = new SignOnScreen(db);
    var user = signOnScreen.Run(app);

    // Esc on the sign-on screen exits the application.
    if (user is null)
    {
        break;
    }

    var mainMenuScreen = new MainMenuScreen(user);
    mainMenuScreen.Run(app);

    // When the menu closes, the loop returns to sign-on.
}
