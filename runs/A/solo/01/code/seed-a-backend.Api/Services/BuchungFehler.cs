namespace seed_a_backend.Api.Services;

public class BuchungNichtGefundenException(string referenz)
    : Exception($"Es wurde keine Buchung mit der Referenz '{referenz}' gefunden.");

public class BuchungBereitsStorniertException(string referenz)
    : Exception($"Die Buchung mit der Referenz '{referenz}' ist bereits storniert.");

public class VeranstaltungNichtGefundenException(string veranstaltungId)
    : Exception($"Es wurde keine Veranstaltung mit der Id '{veranstaltungId}' gefunden.");

public class UngueltigeBuchungException(string nachricht) : Exception(nachricht);

public class SitzplatzKonfliktException(string nachricht) : Exception(nachricht);
