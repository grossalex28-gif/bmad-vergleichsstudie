using System.ComponentModel.DataAnnotations;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Tests.Dtos;

public class CreateOrderRequestDtoTests
{
    private static bool IsValid(CreateOrderRequestDto dto, out List<ValidationResult> results)
    {
        results = [];
        var context = new ValidationContext(dto);
        return Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
    }

    private static CreateOrderRequestDto ValidDto(
        string name = "Jonas", string street = "Hauptstr. 1", string postalCode = "12345",
        string city = "Berlin", string country = "DE", string email = "j@example.com") =>
        new(name, street, postalCode, city, country, email, [new OrderLineRequestDto("P1", "L1", 1)]);

    [Fact]
    public void AllFieldsPopulated_IsValid()
    {
        Assert.True(IsValid(ValidDto(), out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_EmptyOrWhitespaceOnly_IsInvalid(string name)
    {
        var isValid = IsValid(ValidDto(name: name), out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateOrderRequestDto.Name)));
    }

    [Fact]
    public void Email_Empty_IsInvalid()
    {
        var isValid = IsValid(ValidDto(email: ""), out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateOrderRequestDto.Email)));
    }

    [Fact]
    public void Email_WithoutAtSign_IsValid()
    {
        // AD-15: "keine Formatvalidierung über Nicht-Leer hinaus" — bewusst kein [EmailAddress]
        Assert.True(IsValid(ValidDto(email: "keine-email-adresse"), out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Street_EmptyOrWhitespaceOnly_IsInvalid(string street)
    {
        var isValid = IsValid(ValidDto(street: street), out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateOrderRequestDto.Street)));
    }

    [Fact]
    public void PostalCode_Empty_IsInvalid()
    {
        var isValid = IsValid(ValidDto(postalCode: ""), out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateOrderRequestDto.PostalCode)));
    }

    [Fact]
    public void City_Empty_IsInvalid()
    {
        var isValid = IsValid(ValidDto(city: ""), out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateOrderRequestDto.City)));
    }

    [Fact]
    public void Country_Empty_IsInvalid()
    {
        var isValid = IsValid(ValidDto(country: ""), out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateOrderRequestDto.Country)));
    }
}
