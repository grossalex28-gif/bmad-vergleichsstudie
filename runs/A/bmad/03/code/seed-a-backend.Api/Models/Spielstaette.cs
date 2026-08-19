namespace seed_a_backend.Api.Models;

public class Spielstaette
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public ICollection<Raum> Raeume { get; set; } = new List<Raum>();
}
