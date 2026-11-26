using Coaching.Domain.Coaches;
using Coaching.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Persistence.Configurations;

/// <summary>@ADR-f03-thu-vien-bai-tap-007: bảng ExerciseLibrarySeeds (Liquibase v0.5.0-0002).</summary>
internal sealed class ExerciseLibrarySeedConfiguration : IEntityTypeConfiguration<ExerciseLibrarySeed>
{
    public void Configure(EntityTypeBuilder<ExerciseLibrarySeed> builder)
    {
        builder.ToTable("ExerciseLibrarySeeds");
        builder.HasKey(s => s.CoachId);
        builder.Property(s => s.CoachId).ValueGeneratedNever();
        builder.HasOne<Coach>().WithOne().HasForeignKey<ExerciseLibrarySeed>(s => s.CoachId).OnDelete(DeleteBehavior.Restrict);
    }
}
