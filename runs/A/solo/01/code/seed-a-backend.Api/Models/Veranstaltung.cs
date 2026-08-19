namespace seed_a_backend.Api.Models;

public class Veranstaltung
{
    public string Id { get; set; } = default!;
    public string Titel { get; set; } = default!;
    public string Beschreibung { get; set; } = default!;

    public string SpielstaetteId { get; set; } = default!;
    public Spielstaette Spielstaette { get; set; } = default!;

    public string RaumId { get; set; } = default!;
    public Raum Raum { get; set; } = default!;

    public DateTime Zeitpunkt { get; set; }
    public int DauerMinuten { get; set; }
    public int Altersfreigabe { get; set; }

    public ICollection<Preiskategorie> Preiskategorien { get; set; } = new List<Preiskategorie>();
    public ICollection<Buchungsposition> Buchungspositionen { get; set; } = new List<Buchungsposition>();
}
