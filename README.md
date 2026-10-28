# TUIPOS

A small point-of-sale app for the terminal, dressed up like an IBM i (AS/400)
green-screen. You get function keys, dot-leader prompts, a `===>` command line,
and the 80×24 grid.

It's a just-for-fun project, built with .NET 10, Terminal.Gui v2, and SQLite.

## Quick start

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run
```

The first run creates `pos.db` in the current directory, applies the
migrations, and seeds the sample data.

Sign on with:

| User | PIN |
| --- | --- |
| `0000` | `123456` |

Press F3 or Esc on the sign-on screen to quit.

## Keys

| Key | Action |
| --- | --- |
| Enter | Submit the screen |
| F3 | Exit. On the main menu this signs off. On the sign-on screen it quits. |
| F9 | Retrieve a previous command (press again to go further back) |
| F12 | Cancel: clears the command line or goes back a screen |
| Esc | Quits on the sign-on screen. Everywhere else it acts like F12. |

## Main menu

Type an option number on the `===>` line and press Enter.

| Option | Screen |
| --- | --- |
| 1 | Register *(coming soon)* |
| 2 | Item inquiry *(coming soon)* |
| 3 | Sales journal *(coming soon)* |
| 4 | Daily totals *(coming soon)* |
| 5 | Display user information |
| 90 | Sign off (`SIGNOFF` also works) |

## Data

The app uses SQLite through EF Core. Everything lives in `pos.db`:

- **Users** have a user ID, a name, and a hashed six-digit PIN
- **Items** have a name, a barcode, and a price
- **Sales** and **SaleItems** store completed transactions. Line items keep a
  snapshot of the name and price at the time of sale.

Prices are stored as integer cents (for example, `$3.99` is stored as `399`).

To start over, delete `pos.db` and run the app again.

### Migrations

```bash
dotnet tool restore
dotnet ef migrations add <Name>
```

## Project layout

```
Program.cs      entry point: migrate, seed, then the sign-on → menu loop
Data/           DbContext and seeder
models/         EF Core entities
UI/             one class per green-screen
Migrations/     EF Core migrations
```

## Roadmap

- [x] Sign-on screen
- [x] Main menu
- [x] Display user information
- [ ] Register (ring up a sale)
- [ ] Item inquiry
- [ ] Sales journal and daily totals
