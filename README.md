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
| F5 | Refresh a list or status screen |
| F6 | Pay on the register, or create on a "Work with" list |
| F9 | Retrieve a previous command (press again to go further back) |
| F12 | Cancel: clears the command line or goes back a screen |
| Esc | Quits on the sign-on screen. Everywhere else it acts like F12. |

## Main menu

Type an option number on the `===>` line and press Enter.

| Option | Screen |
| --- | --- |
| 1 | Register |
| 2 | Item inquiry *(coming soon)* |
| 3 | Sales journal *(coming soon)* |
| 4 | Daily totals *(coming soon)* |
| 5 | Display user information |
| 6 | Administration (administrators only) |
| 90 | Sign off (`SIGNOFF` also works) |

## Register

- Type or scan a barcode and press Enter. Leave the quantity blank for 1.
  Scanning an item that's already on the sale adds to its line.
- To remove a line, type `4` next to it and press Enter. Use PageUp and
  PageDown to move between pages when there are more than 8 lines.
- Press F6 to pay. Choose 1=Cash, 2=Card or 3=Other. Cash asks for the amount
  tendered and shows the change due. F12 or Esc cancels the payment and keeps
  the sale.
- Press F3 to go back to the menu. If a sale is in progress, press F3 twice to
  discard it.

Completed sales are saved to the `Sales` and `SaleItems` tables.

## Administration

Option 6 opens the `ADMIN` menu. Only administrators can open it; everyone
else gets "Not authorized to Administration." F3 or F12 goes back to the main
menu, and 90 signs off.

| Option | Screen |
| --- | --- |
| 1 | Work with users |
| 2 | Work with items |
| 3 | Change PIN |
| 4 | Display system status |

The two "Work with" lists work like their IBM i namesakes. Type an option next
to a row and press Enter: `2` to change it or `4` to delete it, which asks you
to confirm first. Press F6 to create a new user or item.

- **Users:** you can't delete the user you're signed on as, or a user who has
  sales. At least one user must stay an administrator. A blank PIN on Change
  keeps the current PIN.
- **Items:** deleting an item doesn't change past sales, because each sale
  line keeps its own copy of the name and price.

## Data

The app uses SQLite through EF Core. Everything lives in `pos.db`:

- **Users** have a user ID, a name, a hashed six-digit PIN, and an
  administrator flag
- **Items** have a name, a barcode, and a price
- **Sales** and **SaleItems** store completed transactions. Line items keep a
  snapshot of the name and price at the time of sale.

Prices are stored as integer cents (for example, `$3.99` is stored as `399`).

Sample data only goes into empty tables, so users and items you delete stay
deleted. To start over, delete `pos.db` and run the app again.

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
- [x] Register (ring up a sale)
- [x] Administration: users, items, change PIN, system status
- [ ] Item inquiry
- [ ] Sales journal and daily totals
