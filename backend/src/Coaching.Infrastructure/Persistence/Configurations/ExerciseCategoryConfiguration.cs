using Coaching.Domain.Coaches;
using Coaching.Domain.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Persistence.Configurations;

/// <summary>@ADR-f03-thu-vien-bai-tap-003: bảng ExerciseCategories (Liquibase v0.5.0).</summary>
internal sealed class ExerciseCategoryConfiguration : IEntityTypeConfiguration<ExerciseCategory>
{
    public void Configure(EntityTypeBuilder<ExerciseCategory> builder)
    {
        builder.ToTable("ExerciseCategories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Name).HasMaxLength(ExerciseCategory.NameMaxLength).IsRequired();
        builder.Property(c => c.NameKey).HasMaxLength(ExerciseCategory.NameKeyMaxLength).IsRequired();

        // @BR-f03-thu-vien-bai-tap-006 @ADR-f03-thu-vien-bai-tap-002
        builder.HasIndex(c => new { c.CoachId, c.Kind, c.NameKey }).IsUnique();
        builder.HasOne<Coach>().WithMany().HasForeignKey(c => c.CoachId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(c => c.DomainEvents);
    }
}
