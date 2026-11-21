using Ecommerce.Domain.Coupons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Configurations;
internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");

        builder.HasKey(coupon => coupon.Id);

        builder.Property(coupon => coupon.Id)
               .HasConversion(couponId => couponId.Value, value => new CouponId(value));

        builder.Property(coupon => coupon.Code)
               .HasMaxLength(CouponCode.MaxLength)
               .HasConversion(code => code.Value, value => CouponCode.Create(value).Value);

        builder.HasIndex(coupon => coupon.Code)
               .IsUnique();

        // Two orders that redeem the last use at the same time can't both save.
        builder.Property<uint>("Version")
               .HasColumnName("xmin")
               .HasColumnType("xid")
               .IsRowVersion();

        builder.Property(coupon => coupon.PercentOff);
        builder.Property(coupon => coupon.StartsOnUtc);
        builder.Property(coupon => coupon.ExpiresOnUtc);
        builder.Property(coupon => coupon.MaxRedemptions);
        builder.Property(coupon => coupon.RedemptionCount);
        builder.Property(coupon => coupon.CreatedOnUtc);
        builder.Property(coupon => coupon.UpdatedOnUtc);
    }
}
