# AGENTS.md

Guidance for AI coding agents working in this repository.

## What this is

TUIPOS is a hobby point-of-sale app that runs in the terminal and imitates the
look and feel of an IBM i (AS/400) 5250 green-screen. It's built for fun, not
for production. Don't add warnings or hardening work about credentials, PIN
storage, or the seeded user; they're deliberate.

## Stack

- .NET 10 / C# console app (`tuipos.csproj`), nullable and implicit usings enabled
- Terminal.Gui **2.5.0**: the v2 API. Most docs and examples online are for v1 and won't compile here.
- EF Core 10 with SQLite (`pos.db` in the working directory)
- `Microsoft.Extensions.Identity.Core`, used only for `PasswordHasher<PosUser>` to hash and verify PINs
- `dotnet-ef` 10.0.14 as a local tool (`dotnet-tools.json`)

## Commands

```bash
dotnet build          # must finish with 0 errors before you call a change done
dotnet run            # run from the repo root; pos.db is resolved relative to the CWD
dotnet tool restore   # installs dotnet-ef locally
```

On startup the app migrates the database and seeds it. The seeder is idempotent,
so running it again is safe.

Seeded sign-on: user `0000`, PIN `123456`.

To reset local data, delete `pos.db` (it's gitignored) and run the app again.

## Layout

| Path | Purpose |
| --- | --- |
| `Program.cs` | Entry point: migrates and seeds, creates the `IApplication`, then runs a sign-on → main menu loop |
| `Data/PosBdContext.cs` | `PosDbContext` (the filename has a typo, the class name doesn't) |
| `Data/DBSeeder.cs` | Idempotent seed data: one admin user and 20 grocery items |
| `models/` | EF entities: `Item`, `PosUser`, `Sale`, `SaleItem` (the folder is lowercase, the namespace is `tuipos.Models`) |
| `UI/` | One class per 5250-style screen, plus the shared building blocks described below |
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
- When a `TextField` gets focus again, its text is selected, so the next
  keystroke replaces it.
- `app.AddTimeout` returns `object?`. Pass that token to `app.RemoveTimeout`
  once the screen closes.
- `Terminal.Gui.Drawing.Attribute` collides with `System.Attribute`, so always
  write it fully qualified.
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
  - `InputField(x, y, length, allowed?)`, which refuses input longer than
    `length` or with characters `allowed` rejects (e.g. `char.IsAsciiDigit`),
    and adds one extra cell so the cursor fits after the last character
  - `Amount(cents)` and `TryParseAmount(text, out cents)` for showing and
    reading money
  - `DotLeader(prompt, width)`
  - `OutputLine(x, y, prompt, promptWidth, value)` for read-only
    "prompt `:` value" rows
  - `AddHeader(grid, screenId, title)`
  - `Timestamp(DateTime)`
- **`MessageLine`**: call `Show(text)` for information, `Error(text)` for errors,
  and `Clear()` to empty it.
- **`Messages`**: the catalog of IBM-style message text. IDs are `TPSnnnn` and
  each screen gets its own range: sign-on `00xx`, main menu `01xx`, register
  `02xx`, payment `03xx`. Give each new screen the next unused range.

Every screen follows the same pattern. It's a class whose dependencies come in
through the constructor, with a `Run(IApplication app)` method. That method
creates a `using GreenScreenWindow window`, adds a header and views, hooks
`window.KeyDown` for function keys, and calls `app.Run(window)`. A menu option
opens its screen with `new XScreen(...).Run(app)` inside the command handler.
That starts a nested session, and calling `app.RequestStop()` in the inner
screen returns to the menu. `UserInfoScreen` is the simplest example.

Other patterns to copy:

- **Lists** (IBM subfiles): `RegisterScreen` shows a page of rows as fixed
  label slots, with a 1-character option field beside each row (`4=Remove`).
  PageUp and PageDown move between pages, and "More..." or "Bottom" sits under
  the last row. Options are processed before any new entry on Enter.
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
