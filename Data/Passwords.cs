using Microsoft.AspNetCore.Identity;
using tuipos.Models;

namespace tuipos.Data;

// Passwords work like IBM i at QPWDLVL 0: up to 10 characters and not
// case-sensitive, so they're uppercased before hashing and verifying.
public static class Passwords
{
    public const int MinLength = 6;
    public const int MaxLength = 10;

    private static readonly PasswordHasher<PosUser> Hasher = new();

    public static bool IsAllowed(char c)
    {
        return !char.IsWhiteSpace(c);
    }

    public static bool IsValid(string password)
    {
        return password.Length is >= MinLength and <= MaxLength
            && password.All(IsAllowed);
    }

    public static string Hash(PosUser user, string password)
    {
        return Hasher.HashPassword(user, password.ToUpperInvariant());
    }

    public static bool Verify(PosUser user, string password)
    {
        return Hasher.VerifyHashedPassword(user, user.PinHash, password.ToUpperInvariant())
            != PasswordVerificationResult.Failed;
    }
}
