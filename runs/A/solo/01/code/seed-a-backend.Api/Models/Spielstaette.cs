namespace seed_a_backend.Api.Models;

public class Spielstaette
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;

    public ICollection<Raum> Raeume { get; set; } = new List<Raum>();
}
