namespace seed_a_backend.Api.Data;

/// <summary>
/// Zentralisiert die Europe/Berlin-Zeitzonenlogik, die sowohl der Anfangsdatenbestand-Parser
/// (<see cref="OffsetlosAlsEuropaBerlinZeitpunktConverter"/>) als auch der Datumsbereich-Filter
/// der Veranstaltungsliste benötigen, statt sie zweimal zu duplizieren.
/// </summary>
public static class EuropaBerlinZeitzone
{
    public static readonly TimeZoneInfo Instanz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static DateTimeOffset Tagesbeginn(DateOnly datum)
    {
        var lokaleZeit = datum.ToDateTime(TimeOnly.MinValue);
        var offset = Instanz.GetUtcOffset(lokaleZeit);
        if (lokaleZeit.Ticks - offset.Ticks < DateTime.MinValue.Ticks)
        {
            return DateTimeOffset.MinValue;
        }
        return new DateTimeOffset(lokaleZeit, offset);
    }

    public static DateTimeOffset Tagesende(DateOnly datum)
    {
        var lokaleZeit = datum.ToDateTime(TimeOnly.MaxValue);
        var offset = Instanz.GetUtcOffset(lokaleZeit);
        if (lokaleZeit.Ticks - offset.Ticks > DateTime.MaxValue.Ticks)
        {
            return DateTimeOffset.MaxValue;
        }
        return new DateTimeOffset(lokaleZeit, offset);
    }
}
