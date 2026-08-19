namespace seed_a_backend.Api.Models;

public class Veranstaltung
{
    public string Id { get; set; } = string.Empty;
    public string Titel { get; set; } = string.Empty;
    public string Beschreibung { get; set; } = string.Empty;

    public string SpielstaetteId { get; set; } = string.Empty;
    public Spielstaette? Spielstaette { get; set; }

    public string RaumId { get; set; } = string.Empty;
    public Raum? Raum { get; set; }

    public DateTime Zeitpunkt { get; set; }
    public int DauerMinuten { get; set; }
    public int Altersfreigabe { get; set; }

    public ICollection<Preiskategorie> Preiskategorien { get; set; } = new List<Preiskategorie>();
    public ICollection<Buchung> Buchungen { get; set; } = new List<Buchung>();
}
