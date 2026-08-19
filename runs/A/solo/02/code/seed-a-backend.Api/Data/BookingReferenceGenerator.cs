using System.Security.Cryptography;

namespace seed_a_backend.Api.Data;

public static class BookingReferenceGenerator
{
    // Excludes visually ambiguous characters (0/O, 1/I).
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Length = 8;

    public static string Generate()
    {
        Span<char> buffer = stackalloc char[Length];
        for (var i = 0; i < Length; i++)
        {
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(buffer);
    }
}
