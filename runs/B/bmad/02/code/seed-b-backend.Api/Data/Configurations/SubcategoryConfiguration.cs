using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Data.Configurations;

public class SubcategoryConfiguration : IEntityTypeConfiguration<Subcategory>
{
    public void Configure(EntityTypeBuilder<Subcategory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasMany(x => x.Properties)
            .WithOne(x => x.Subcategory)
            .HasForeignKey(x => x.SubcategoryId);

        builder.HasMany(x => x.Products)
            .WithOne(x => x.Subcategory)
            .HasForeignKey(x => x.SubcategoryId);
    }
}
