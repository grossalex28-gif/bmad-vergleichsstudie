namespace seed_b_backend.Api.Models;

public class Order
{
    public int Id { get; set; }
    public required string Status { get; set; }
    public DateTime ErstelltAm { get; set; }

    public required string LieferName { get; set; }
    public required string LieferStrasse { get; set; }
    public required string LieferPlz { get; set; }
    public required string LieferOrt { get; set; }
    public required string LieferLand { get; set; }

    public required string KontaktEmail { get; set; }
    public required string KontaktTelefon { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}
