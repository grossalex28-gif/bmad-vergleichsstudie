namespace seed_a_backend.Api.Services;

public class NotFoundException(string message) : Exception(message);

public class ValidationException(string message) : Exception(message);

public class SeatConflictException(IReadOnlyList<(string Reihe, int Spalte)> belegteSitzplaetze)
    : Exception("Mindestens ein ausgewählter Sitzplatz wurde zwischenzeitlich belegt.")
{
    public IReadOnlyList<(string Reihe, int Spalte)> BelegteSitzplaetze { get; } = belegteSitzplaetze;
}
