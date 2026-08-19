using System.Security.Cryptography;

namespace seed_a_backend.Api.Application;

public static class BookingReferenceGenerator
{
    // Ohne 0/O, 1/I/L (AD-3) — 31 Zeichen, log2(31) ≈ 4.954 Bit/Zeichen.
    private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    // ceil(128 / log2(31)) = 26 Zeichen ⇒ ≥128 Bit Entropie (NFR-4, "Größenordnung UUID v4").
    private const int Length = 26;

    public static string Generate() => RandomNumberGenerator.GetString(Alphabet, Length);
}
