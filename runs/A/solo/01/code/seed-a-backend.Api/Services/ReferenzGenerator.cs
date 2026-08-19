using System.Security.Cryptography;

namespace seed_a_backend.Api.Services;

public static class ReferenzGenerator
{
    // Ohne leicht verwechselbare Zeichen (0/O, 1/I/L).
    private const string Zeichen = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int Laenge = 8;

    public static string NeueReferenz()
    {
        Span<char> puffer = stackalloc char[Laenge];
        for (var i = 0; i < Laenge; i++)
        {
            puffer[i] = Zeichen[RandomNumberGenerator.GetInt32(Zeichen.Length)];
        }

        return new string(puffer);
    }
}
