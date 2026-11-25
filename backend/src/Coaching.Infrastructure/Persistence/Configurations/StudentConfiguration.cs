using Coaching.Domain.Coaches;
using Coaching.Domain.Common;
using Coaching.Domain.Students;
using Coaching.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Persistence.Configurations;

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.FullName).HasMaxLength(Student.FullNameMaxLength).IsRequired();
        builder.Property(s => s.Email)
            .HasConversion(e => e.Value, v => Email.Create(v))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();
        // @BR-f02-ho-so-hoc-vien-001: chốt chặn cuối; kiểm trùng chính nằm ở Identity (gồm cả tài khoản coach)
        builder.HasIndex(s => s.Email).IsUnique();

        builder.Property(s => s.AccountStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.CoachingStatus).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.UserId).IsUnique();
        builder.HasIndex(s => new { s.CoachId, s.CreatedAt });
        builder.HasOne<Coach>().WithMany().HasForeignKey(s => s.CoachId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithOne().HasForeignKey<Student>(s => s.UserId).OnDelete(DeleteBehavior.Restrict);

        // @ADR-f02-ho-so-hoc-vien-002: toàn bộ mục form trong 1 cột JSON
        builder.OwnsOne(s => s.Registration, r => r.ToJson("Registration"));
        builder.Navigation(s => s.Registration).IsRequired();
        builder.Ignore(s => s.MissingRequiredFields);

        builder.Ignore(s => s.DomainEvents);
    }
}
