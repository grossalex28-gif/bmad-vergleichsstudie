namespace seed_b_backend.Api.Models;

public class Product
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Beschreibung { get; set; }
    public required string SubcategoryId { get; set; }

    public Subcategory? Subcategory { get; set; }

    // Category-specific properties, stored as a raw JSON object (key -> value).
    // The set of keys is defined by the owning Subcategory.Eigenschaften.
    public string EigenschaftenJson { get; set; } = "{}";

    public int Aufrufe { get; set; }

    public List<Offer> Offers { get; set; } = [];
    public List<Rating> Ratings { get; set; } = [];
}
