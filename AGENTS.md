# AGENTS.md

Guidance for AI coding agents working in this repository.

## What this is

TUIPOS is a hobby point-of-sale app that runs in the terminal and imitates the
look and feel of an IBM i (AS/400) 5250 green-screen. It's built for fun, not
for production. Don't add warnings or hardening work about credentials, password
storage, or the seeded user; they're deliberate.

## Stack

- .NET 10 / C# console app (`tuipos.csproj`), nullable and implicit usings enabled
- Terminal.Gui **2.5.0**: the v2 API. Most docs and examples online are for v1 and won't compile here.
- EF Core 10 with SQLite (`pos.db` in the working directory)
- `Microsoft.Extensions.Identity.Core`, used only for `PasswordHasher<PosUser>` to hash and verify passwords
- `QuestPDF` 2026.9.1 for PDF exports, under its free Community license (set in `Program.cs`)
- `ZXing.Net` 0.16.11, used only to encode Code 128 barcodes for the PDFs
- `dotnet-ef` 10.0.14 as a local tool (`dotnet-tools.json`)

## Commands

```bash
dotnet build          # must finish with 0 errors before you call a change done
dotnet run            # run from the repo root; pos.db is resolved relative to the CWD
dotnet tool restore   # installs dotnet-ef locally
```

On startup the app migrates the database and seeds it. The seeder is idempotent,
so running it again is safe. It only seeds empty tables, so users and items
deleted in Administration stay deleted. It also makes sure at least one user is
an administrator, promoting the first user if none is.

Seeded sign-on: user `QSECOFR`, password `QSECOFR`.

To reset local data, delete `pos.db` (it's gitignored) and run the app again.

## Layout

| Path | Purpose |
| --- | --- |
| `Program.cs` | Entry point: migrates and seeds, creates the `IApplication`, then runs a sign-on → main menu loop |
| `Data/PosBdContext.cs` | `PosDbContext` (the filename has a typo, the class name doesn't) |
| `Data/DBSeeder.cs` | Idempotent seed data for empty tables: admin user `QSECOFR` and 20 grocery items |
| `Data/Passwords.cs` | Password rules, plus hashing and verifying them |
| `models/` | EF entities: `Item`, `PosUser`, `Sale`, `SaleItem` (the folder is lowercase, the namespace is `tuipos.Models`) |
| `UI/` | One class per 5250-style screen, plus the shared building blocks described below |
| `Reports/` | Report layouts (`ReportWriter` plus one class per report) and `SpoolStore`, which saves report output as spooled files |
| `spool/` | Spooled files at runtime, next to `pos.db` (gitignored) |
| `export/` | PDF exports at runtime, next to `pos.db` (gitignored) |
| `Migrations/` | EF Core migrations. Generated code, don't hand-edit. |

Leave the odd file and folder names (`PosBdContext.cs`, `DBSeeder.cs`, `models/`)
alone unless you're asked to rename them.

## Hard rules

- **Don't change the database schema unless you're explicitly asked.** That covers
  entity properties in `models/`, `OnModelCreating`, and adding or editing
  migrations. Reading and writing rows is fine.
- Money is stored as integer cents (`long ...Cents`). Never use `decimal` or `double`
  for stored amounts. Format to dollars only for display.
- `SaleItem` holds snapshot values (name, barcode, price), so sales history
  survives later catalog edits. Keep it that way.
- `PosUser.IsAdmin` controls access to the Administration menu. Keep at least
  one administrator: WRKUSR and CHGUSR both refuse to remove the last one.
- Sign-on works like IBM i. User profiles are up to 10 characters (letters,
  digits, `$ # @ _`, not starting with a digit or `_`) and stored in uppercase.
  Passwords are 6 to 10 characters and not case-sensitive, so always hash and
  verify them through `Passwords`, which uppercases them. The hash lives in
  `PosUser.PinHash`, a column name left over from PIN sign-on.
- The `PosDbContext` lives for the whole app. Read with `AsNoTracking()`, and
  call `ChangeTracker.Clear()` after saving (or use `ExecuteUpdate` /
  `ExecuteDelete`), so entities don't stay tracked between screens.
- SQLite can't compare `DateTimeOffset` values in a query. Filter by date in
  memory after selecting the columns you need (see `SystemStatusScreen`).

## Terminal.Gui v2 notes

- The app lifetime belongs to `Program.cs`: `Application.Create()` returns an
  `IApplication`, then you call `app.Init()` once. Screens take that `app` and call
  `app.Run(window)`. Screens must not create or init their own application.
- `Window` derives from `Runnable`, which derives from `View`. To leave a screen,
  call `app.RequestStop()`. To hand a value back, either capture it in a field
  before stopping or derive from `Runnable<T>` and set `Result`.
- By default, Esc stops the running `Runnable` (it's the application quit key).
- Handle keys with the `KeyDown` event (`key.Handled = true` to consume a key) and
  compare against `Key.F3`, `Key.F12`, `Key.Enter`, `Key.Esc`, and so on.
- A key goes to the focused view first, then up through each superview to the
  window. So a function-key handler on the window sees F-keys typed into a field.
- Enter in a `TextField` raises `Accepting`. Set `e.Handled = true` there, or the
  event keeps bubbling up. Buttons raise `Accepted` and `Accepting`, not v1's `Clicked`.
- To reject an edit, handle `TextField.TextChanging` and set
  `e.Handled = true`. `e.Result` holds the proposed text.
- `TextField` has no `MaxLength`. Enforce lengths in `TextChanging` (which
  `GreenScreen.InputField` already does).
- Setting `Visible = false` on a field also takes it out of the Tab order.
- When a `TextField` gains focus, its text is selected, so the next keystroke
  replaces it. Calling `SetFocus()` on a field that already has focus doesn't
  select anything.
- `app.AddTimeout` returns `object?`. Pass that token to `app.RemoveTimeout`
  once the screen closes.
- `Terminal.Gui.Drawing.Attribute` collides with `System.Attribute`, so always
  write it fully qualified.
- Terminal.Gui 2.5.0 crashes with a `NullReferenceException` in `Key..ctor` on
  any `ESC [ n ~` key missing from its table, including F13-F20 (`ESC[25~` to
  `ESC[34~`). `FunctionKeyPatch.Apply(app)`, called in `Program.cs` right after
  `app.Init()`, adds F13-F20 and rxvt's Home/End (7 and 8) to the table through
  reflection. After a Terminal.Gui upgrade, send those raw sequences through
  tmux (`send-keys -l`) and remove the patch if the library handles them.
- Useful namespaces: `Terminal.Gui.App`, `Terminal.Gui.Views`,
  `Terminal.Gui.Drawing`, `Terminal.Gui.Input`, `Terminal.Gui.ViewBase`.
- If you're unsure an API exists, check the XML docs at
  `~/.nuget/packages/terminal.gui/2.5.0/lib/*/Terminal.Gui.xml` instead of guessing.

## 5250 look and feel

Screens should feel like an IBM i green-screen:

- An 80×24 character grid with absolute positions. No borders, no mouse-first widgets.
- Bright green (`Color.BrightGreen`) for prompts and normal text, white for titles and displayed values,
  blue for the function-key legend, and bright red for error messages.
  Input fields are underlined. All of these colors are on black.
- A screen or menu ID in the top-left, the title centered, and `System: TUIPOS`
  in the top-right.
- Dot-leader prompts, like `User  . . . . . . . . . . :`.
- On menus, a `Selection or command` prompt with an `===>` input line.
- A message line near the bottom, with the function-key legend on the last line
  (`F3=Exit   F12=Cancel`).
- Function keys are the controls: Enter submits, F3 exits, F12 cancels or goes
  back. Avoid visible buttons where a function key would do the job.

## UI building blocks

Build new screens from these pieces instead of styling views one by one:

- **`GreenScreenWindow`**: create one with `new("F3=Exit   F12=Cancel")`. It's a
  borderless black window with a centered 80×24 `Grid` (add your views there, at
  5250 coordinates), a `Message` line on row 22, and the function-key legend on
  row 23. Call `window.RunWithClock(app)` instead of `app.Run(window)` to show
  a live date and time in the top-right corner.
- **`GreenScreen`** (static) provides:
  - The color schemes: `Normal`, `Bright`, `FunctionKeys`, `Error`, `Input`
  - `Text(x, y, text, scheme?)`
  - `InputField(x, y, length, allowed?, monocase?)`, which refuses input longer
    than `length` or with characters `allowed` rejects (e.g. `char.IsAsciiDigit`),
    and adds one extra cell so the cursor fits after the last character.
    `monocase: true` capitalizes letters as they're typed, as for user profiles.
  - `IsNameChar(c)` and `IsValidName(text)` for IBM i names like user profiles
  - `Amount(cents)` and `TryParseAmount(text, out cents)` for showing and
    reading money
  - `DotLeader(prompt, width)`
  - `OutputLine(x, y, prompt, promptWidth, value)` for read-only
    "prompt `:` value" rows
  - `AddHeader(grid, screenId, title)`
  - `Timestamp(DateTime)`
  - `Clip(text, length)` for list columns
  - `YesNo(bool)` and `ParseYesNo(text)` for IBM's `*YES` / `*NO` values
- **`MenuScreen`**: a numbered menu with the `===>` command line, F9=Retrieve,
  `SIGNOFF` and option 90. Add options with `AddOption(number, text, handler)`.
  The handler gets a `MenuSelection` with the app, the menu's message line and
  the option number. With `isInitialMenu: true` (MAIN), F3 signs off. Submenus
  (ADMIN) go back on F3 or F12 and return `MenuResult.SignOff` when the user
  signs off from them, so the caller should set `selection.SignOff`.
- **`OptionList<T>`**: a page of rows with a 1-character option field beside
  each, like an IBM subfile. Call `SetItems`, `AddTo(grid)` (after any entry
  fields so Tab moves down the page), and `OnEnter(submit)`. Then read
  `TypedOptions()` in your submit handler, and call `PageUp()` / `PageDown()`
  from your key handler. It shows "More..." or "Bottom" under the last row.
- **`ConfirmDeleteScreen`**: IBM's "Confirm Delete" step for rows marked with
  `4=Delete`. Returns true when the user presses Enter.
- **`MessageLine`**: call `Show(text)` for information, `Error(text)` for errors,
  and `Clear()` to empty it.
- **`Messages`**: the catalog of IBM-style message text. IDs are `TPSnnnn` and
  each screen gets its own range: sign-on `00xx`, menus `01xx`, register
  `02xx`, payment `03xx`, users `04xx`, items `05xx`, change password `06xx`,
  reports `07xx`, item inquiry and select `08xx`, and shared messages `09xx`.
  Give each new screen the next unused range (`10xx` is next).

Every screen follows the same pattern. It's a class whose dependencies come in
through the constructor, with a `Run(IApplication app)` method. That method
creates a `using GreenScreenWindow window`, adds a header and views, hooks
`window.KeyDown` for function keys, and calls `app.Run(window)`. A menu option
opens its screen with `new XScreen(...).Run(app)` inside the command handler.
That starts a nested session, and calling `app.RequestStop()` in the inner
screen returns to the menu. `UserInfoScreen` is the simplest example.

Other patterns to copy:

- **Work with lists**: `WorkWithUsersScreen` and `WorkWithItemsScreen` show
  the IBM layout: "Type options, press Enter.", the options legend in blue, the
  `Opt` column, F5=Refresh and F6=Create. On Enter, they check every typed
  option first, then handle 2=Change in order down the page, then confirm and
  run 4=Delete.
- **Prompt screens**: `UserEditScreen` and `ItemEditScreen` handle both Create
  and Change ("Type choices, press Enter."). On Change, the key field (user
  profile or barcode) is shown but can't be edited. Errors put the cursor on
  the field to fix.
- **Lists with an entry line**: `RegisterScreen` puts an `OptionList` under its
  barcode entry, and processes typed options before any new entry on Enter.
- **F4=Prompt**: `ItemSelectScreen` (SLTITM) is IBM's prompt list for a
  barcode field. It searches by part of the description or the start of the
  barcode, takes one `1=Select`, and returns the `Item` (or null on F3/F12).
  `RegisterScreen` and `ItemInquiryScreen` open it on F4, pass what's in the
  barcode field as the starting search, and put the result back.

## Reports and spooled files

Reports work like IBM i printer output. A report never prints directly. It's
saved as a spooled file, a plain text file in `spool/`, and then shown with
`DisplaySpooledFileScreen`.

- **`ReportWriter`** lays out pages like an IBM printer file: 132 columns, a
  66-line form with overflow at line 60, the page heading (report ID, title,
  page number, system, user, timestamp) on every page, `Subheading` and
  `ColumnHeadings` repeated on each page, CRLF line endings, a form feed after
  every page, and IBM's END OF LISTING line from `Finish()`. Keep report text
  plain ASCII. The target printer is an Okidata Pacemark 4410 (wide carriage,
  132 columns, Epson FX / IBM ProPrinter III / OKI Microline emulation), which
  prints these files raw.
- **One static class per report** in `Reports/`, with `Id` (up to 10 capital
  letters or digits, e.g. `SLSJRN`), `Title`, and a `Build(...)` that returns a
  `Report`. Use `SalesData.Between(db, from, to)` for sales in a date range,
  and `SalesData.DateText` / `TimeText` for culture-independent dates.
- **`SpoolStore`** saves files as `000001-ID-USER.txt` and reads the number,
  report, user and page count (one form feed per page) back from them.
- **To add a report:** write the report class, then add it to
  `ReportsMenuScreen` with `RunDated(...)` (which shows `ReportPromptScreen`
  for a date or date range) or `Spool(...)` if it takes no input.

### PDF exports

PDFs are for an office printer, so they skip the spool: spooled files stay
plain text for the Okidata. `ItemBarcodeExport` (Reports option 6) is the
first one. It writes `export/ITMBAR-yyyyMMdd-HHmmss.pdf`, US Letter, with the
same page heading as the text reports, and shows the path on the message line
(`TPS0705`). If the export fails, the message line shows the error (`TPS0706`)
and the app keeps running.

- Build PDFs with QuestPDF's fluent API. `QuestPDF.Settings.License` must be
  set before the first PDF, or QuestPDF throws. `Program.cs` sets it at startup.
- `Barcode.Code128(value, height)` draws a barcode as SVG for QuestPDF's
  `.Svg()`, including the blank margin scanners need. One SVG unit is one bar
  width, so `Width(barcode.Width)` gives 1-point bars. Code 128 is used because
  item barcodes don't always have a valid UPC/EAN check digit.
- To check a PDF, render it with Ghostscript (`gs -sDEVICE=pnggray -r300`)
  and look at the pages.
- **Pop-up windows**: `PaymentWindow` is a plain `Window` smaller than the
  screen, run with `app.Run` from inside the screen below it, which stays
  visible behind. Its dotted IBM border is drawn as full-width label rows added
  first, so they also blank out the screen underneath. It returns its result
  the same way `SignOnScreen` does.

## Code style

Match the existing code:

- File-scoped namespaces (`namespace tuipos.UI;`)
- Private fields are `_camelCase`, injected through the constructor
- Declare Terminal.Gui views with an explicit type and an object initializer
  (`Label heading = new() { X = 3, Y = 2, Text = "..." };`). Use `var` for EF
  queries and locals.
- Comments are short and say *why*, one line above the code they explain
- Allman braces, 4-space indent

## Verifying UI changes

The TUI needs a real terminal. `tmux` is available, so you can run
`dotnet run` in a detached tmux session, use `send-keys` to type input, and use
`capture-pane -p` to read the screen. That's a better check than assuming a
layout renders correctly.

The database path is relative to the working directory. To test without
adding test sales to the real `pos.db`, run the built DLL from a scratch folder
(`dotnet <repo>/bin/Debug/net10.0/tuipos.dll`). It creates and seeds a fresh
database there.
