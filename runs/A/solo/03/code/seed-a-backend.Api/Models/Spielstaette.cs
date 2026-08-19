namespace seed_a_backend.Api.Models;

public class Spielstaette
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Raum> Raeume { get; set; } = new List<Raum>();
}
