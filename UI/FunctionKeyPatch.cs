using System.Reflection;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;

namespace tuipos.UI;

// Terminal.Gui 2.5.0 crashes on any "ESC [ n ~" key it has no entry for,
// which includes F13-F20 (the 5250 F19=Left and F20=Right) and rxvt's Home
// and End. This adds them to the parser's private key table. Remove it once
// a Terminal.Gui release handles these keys itself.
public static class FunctionKeyPatch
{
    private static readonly Dictionary<int, Key> MissingKeys = new()
    {
        [7] = Key.Home,
        [8] = Key.End,
        [25] = Key.F13,
        [26] = Key.F14,
        [28] = Key.F15,
        [29] = Key.F16,
        [31] = Key.F17,
        [32] = Key.F18,
        [33] = Key.F19,
        [34] = Key.F20
    };

    // Call after app.Init(). Returns false if the parser wasn't found, e.g.
    // on another driver or a Terminal.Gui version with different internals.
    public static bool Apply(IApplication app)
    {
        object? inputProcessor = Member(app.Driver, "_inputProcessor");
        object? parser = Member(inputProcessor, "Parser");
        object? keyboardParser = Member(parser, "_keyboardParser");

        if (Member(keyboardParser, "_patterns") is not IEnumerable<AnsiKeyboardParserPattern> patterns)
        {
            return false;
        }

        bool patched = false;

        foreach (var pattern in patterns.OfType<CsiKeyPattern>())
        {
            if (Member(pattern, "_keyCodeMap") is Dictionary<int, Key> keyCodes)
            {
                // TryAdd keeps any mapping a later Terminal.Gui adds itself.
                foreach (var (code, key) in MissingKeys)
                {
                    keyCodes.TryAdd(code, key);
                }

                patched = true;
            }
        }

        return patched;
    }

    // Reads a field or property, public or not. Private members can be
    // declared on a base class, so walk up the types.
    private static object? Member(object? target, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic
            | BindingFlags.Public | BindingFlags.DeclaredOnly;

        for (var type = target?.GetType(); type is not null; type = type.BaseType)
        {
            if (type.GetField(name, flags) is { } field)
            {
                return field.GetValue(target);
            }

            if (type.GetProperty(name, flags) is { } property)
            {
                return property.GetValue(target);
            }
        }

        return null;
    }
}
