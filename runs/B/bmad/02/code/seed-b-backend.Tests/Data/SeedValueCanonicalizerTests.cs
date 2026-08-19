using System.Text.Json;
using seed_b_backend.Api.Data;

namespace seed_b_backend.Tests.Data;

public class SeedValueCanonicalizerTests
{
    private static JsonElement ParseElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
    [Fact]
    public void Canonicalize_BoolTrue_ReturnsLowercaseTrue()
    {
        Assert.Equal("true", SeedValueCanonicalizer.Canonicalize(true));
    }

    [Fact]
    public void Canonicalize_BoolFalse_ReturnsLowercaseFalse()
    {
        Assert.Equal("false", SeedValueCanonicalizer.Canonicalize(false));
    }

    [Fact]
    public void Canonicalize_Int_ReturnsInvariantString()
    {
        Assert.Equal("900", SeedValueCanonicalizer.Canonicalize(900));
    }

    [Fact]
    public void Canonicalize_Double_ReturnsInvariantString()
    {
        Assert.Equal("1.5", SeedValueCanonicalizer.Canonicalize(1.5));
    }

    [Fact]
    public void Canonicalize_String_ReturnsSameString()
    {
        Assert.Equal("In-Ear", SeedValueCanonicalizer.Canonicalize("In-Ear"));
    }

    [Fact]
    public void Canonicalize_Null_ReturnsNull()
    {
        Assert.Null(SeedValueCanonicalizer.Canonicalize(null));
    }

    [Fact]
    public void Canonicalize_JsonElementTrue_ReturnsLowercaseTrue()
    {
        Assert.Equal("true", SeedValueCanonicalizer.Canonicalize(ParseElement("true")));
    }

    [Fact]
    public void Canonicalize_JsonElementFalse_ReturnsLowercaseFalse()
    {
        Assert.Equal("false", SeedValueCanonicalizer.Canonicalize(ParseElement("false")));
    }

    [Fact]
    public void Canonicalize_JsonElementString_ReturnsSameString()
    {
        Assert.Equal("In-Ear", SeedValueCanonicalizer.Canonicalize(ParseElement("\"In-Ear\"")));
    }

    [Fact]
    public void Canonicalize_JsonElementInteger_ReturnsInvariantString()
    {
        Assert.Equal("900", SeedValueCanonicalizer.Canonicalize(ParseElement("900")));
    }

    [Fact]
    public void Canonicalize_JsonElementDecimal_ReturnsInvariantString()
    {
        Assert.Equal("1.5", SeedValueCanonicalizer.Canonicalize(ParseElement("1.5")));
    }

    [Fact]
    public void Canonicalize_JsonElementDecimalWithTrailingZero_PreservesTrailingZero()
    {
        // Regression: P9.KapazitaetLiter im echten Anfangsdatenbestand ist 1.0; GetDouble().ToString()
        // wuerde die Nachkommastelle verwerfen und "1" statt "1.0" liefern.
        Assert.Equal("1.0", SeedValueCanonicalizer.Canonicalize(ParseElement("1.0")));
    }

    [Fact]
    public void Canonicalize_JsonElementNull_ReturnsNull()
    {
        Assert.Null(SeedValueCanonicalizer.Canonicalize(ParseElement("null")));
    }
}
