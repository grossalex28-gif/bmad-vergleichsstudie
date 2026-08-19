using System.ComponentModel.DataAnnotations;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Tests.Dtos;

public class RatingRequestDtoTests
{
    // Ein nicht-ganzzahliger Wert (z. B. 3.5) scheitert bereits an der JSON-Deserialisierung auf
    // `int`, bevor DataAnnotations-Validierung überhaupt greift (System.Text.Json-Verhalten, kein
    // Anwendungscode) — das ist hier daher nicht als Validator.TryValidateObject-Fall abbildbar.
    private static bool IsValid(RatingRequestDto dto, out List<ValidationResult> results)
    {
        results = [];
        var context = new ValidationContext(dto);
        return Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Value_OutsideOneToFive_IsInvalid(int value)
    {
        var dto = new RatingRequestDto("Jonas", value);

        var isValid = IsValid(dto, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RatingRequestDto.Value)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Value_OnRangeBoundary_IsValid(int value)
    {
        var dto = new RatingRequestDto("Jonas", value);

        var isValid = IsValid(dto, out _);

        Assert.True(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AuthorName_EmptyOrWhitespaceOnly_IsInvalid(string authorName)
    {
        var dto = new RatingRequestDto(authorName, 5);

        var isValid = IsValid(dto, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RatingRequestDto.AuthorName)));
    }

    [Fact]
    public void AuthorName_LongerThanMaxLength_IsInvalid()
    {
        var dto = new RatingRequestDto(new string('a', 451), 5);

        var isValid = IsValid(dto, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RatingRequestDto.AuthorName)));
    }
}
