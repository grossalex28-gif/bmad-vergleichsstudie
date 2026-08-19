using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Data.Configurations;

public class SubcategoryPropertyConfiguration : IEntityTypeConfiguration<SubcategoryProperty>
{
    public void Configure(EntityTypeBuilder<SubcategoryProperty> builder)
    {
        builder.HasKey(x => new { x.SubcategoryId, x.Name });
    }
}
