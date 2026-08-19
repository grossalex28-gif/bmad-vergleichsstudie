using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Data.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.HasKey(x => new { x.ProductId, x.SupplierId });
        builder.Property(x => x.Price).HasPrecision(18, 2);

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.Offers)
            .HasForeignKey(x => x.SupplierId);
    }
}
