using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Dtos;

public record CreateOrderRequestDto(
    [property: Required(AllowEmptyStrings = false)] string Name,
    [property: Required(AllowEmptyStrings = false)] string Street,
    [property: Required(AllowEmptyStrings = false)] string PostalCode,
    [property: Required(AllowEmptyStrings = false)] string City,
    [property: Required(AllowEmptyStrings = false)] string Country,
    [property: Required(AllowEmptyStrings = false)] string Email,
    IReadOnlyList<OrderLineRequestDto> Items);
