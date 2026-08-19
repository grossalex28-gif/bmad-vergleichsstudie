using System.Security.Cryptography;

namespace seed_a_backend.Api.Services;

public class ReferenzGenerator : IReferenzGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"; // Crockford Base32, ohne I L O U
    private const int Laenge = 8;

    public string Naechste()
    {
        Span<char> buffer = stackalloc char[Laenge];
        for (var i = 0; i < Laenge; i++)
        {
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(buffer);
    }
}
