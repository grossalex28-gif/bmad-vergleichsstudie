namespace seed_a_backend.Api.Models;

public class Raum
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public required string SpielstaetteId { get; set; }

    public Spielstaette? Spielstaette { get; set; }

    public string[] Reihen { get; set; } = [];

    public required int Spalten { get; set; }

    public int[] GangSpalten { get; set; } = [];

    public ICollection<Veranstaltung> Veranstaltungen { get; set; } = new List<Veranstaltung>();
}
