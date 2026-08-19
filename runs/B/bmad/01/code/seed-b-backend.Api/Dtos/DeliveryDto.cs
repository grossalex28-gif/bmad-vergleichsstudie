using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Dtos;

public class DeliveryDto
{
    [Required] public string Name { get; set; } = null!;
    [Required] public string Street { get; set; } = null!;
    [Required] public string PostalCode { get; set; } = null!;
    [Required] public string City { get; set; } = null!;
    [Required] public string Country { get; set; } = null!;
    [Required] [EmailAddress] public string Email { get; set; } = null!;
    public string? Phone { get; set; }
}
