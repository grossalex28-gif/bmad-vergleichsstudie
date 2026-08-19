using System.Reflection;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Tests;

/// <summary>Bewacht AD-5: Domain/ bleibt frei von EF-Core-/ASP.NET-Core-Typen und -Attributen.</summary>
public class DomainDependencyDirectionTests
{
    private static readonly Type[] DomainTypes =
    [
        typeof(Venue), typeof(Room), typeof(Event), typeof(PriceCategory), typeof(Booking), typeof(BookingPosition),
    ];

    [Fact]
    public void Domain_Typen_haben_keine_Properties_mit_EF_Core_oder_AspNetCore_Typen()
    {
        foreach (var type in DomainTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.False(IsForbiddenNamespace(property.PropertyType.Namespace),
                    $"{type.Name}.{property.Name} referenziert einen unerlaubten Typ aus '{property.PropertyType.Namespace}'.");
            }
        }
    }

    [Fact]
    public void Domain_Typen_tragen_keine_Datenannotationen()
    {
        foreach (var type in DomainTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var attribute in property.GetCustomAttributes())
                {
                    Assert.False(IsForbiddenNamespace(attribute.GetType().Namespace),
                        $"{type.Name}.{property.Name} trägt ein Attribut aus '{attribute.GetType().Namespace}'.");
                }
            }
        }
    }

    private static bool IsForbiddenNamespace(string? ns) =>
        ns is not null && (ns.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
            || ns.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
            || ns.StartsWith("System.ComponentModel.DataAnnotations", StringComparison.Ordinal));
}
