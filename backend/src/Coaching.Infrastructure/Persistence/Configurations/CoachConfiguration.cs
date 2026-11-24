using Coaching.Domain.Coaches;
using Coaching.Domain.Common;
using Coaching.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Persistence.Configurations;

internal sealed class CoachConfiguration : IEntityTypeConfiguration<Coach>
{
    public void Configure(EntityTypeBuilder<Coach> builder)
    {
        builder.ToTable("Coaches");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.DisplayName).HasMaxLength(Coach.DisplayNameMaxLength).IsRequired();
        builder.Property(c => c.Email)
            .HasConversion(e => e.Value, v => Email.Create(v))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.HasIndex(c => c.UserId).IsUnique();
        builder.HasOne<AppUser>().WithOne().HasForeignKey<Coach>(c => c.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(c => c.DomainEvents);
    }
}
