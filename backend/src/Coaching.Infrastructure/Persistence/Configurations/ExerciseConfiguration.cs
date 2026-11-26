using Coaching.Domain.Coaches;
using Coaching.Domain.Exercises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Persistence.Configurations;

/// <summary>@ADR-f03-thu-vien-bai-tap-001: bảng Exercises (Liquibase v0.5.0).</summary>
internal sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercises");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).HasMaxLength(Exercise.NameMaxLength).IsRequired();
        builder.Property(e => e.NameKey).HasMaxLength(Exercise.KeyMaxLength).IsRequired();
        builder.Property(e => e.SearchKey).HasMaxLength(Exercise.KeyMaxLength).IsRequired();
        builder.Property(e => e.VideoUrl).HasMaxLength(Exercise.VideoUrlMaxLength);
        builder.Property(e => e.TechniqueNotes).HasMaxLength(Exercise.NotesMaxLength);
        builder.Property(e => e.CommonMistakes).HasMaxLength(Exercise.NotesMaxLength);
        builder.Ignore(e => e.HasVideo);

        // @ADR-f03-thu-vien-bai-tap-006: liều mặc định là các cột int NULL của chính bảng Exercises
        builder.ComplexProperty(e => e.Dose, dose =>
        {
            dose.Property(d => d.Sets).HasColumnName("DefaultSets");
            dose.Property(d => d.RepsMin).HasColumnName("DefaultRepsMin");
            dose.Property(d => d.RepsMax).HasColumnName("DefaultRepsMax");
            dose.Property(d => d.RirMin).HasColumnName("DefaultRirMin");
            dose.Property(d => d.RirMax).HasColumnName("DefaultRirMax");
            dose.Property(d => d.RestSeconds).HasColumnName("DefaultRestSeconds");
        });

        // @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-001: chốt chặn cuối khi hai request đua
        builder.HasIndex(e => new { e.CoachId, e.NameKey, e.EquipmentId }).IsUnique();
        builder.HasIndex(e => new { e.CoachId, e.IsHidden });
        builder.HasOne<Coach>().WithMany().HasForeignKey(e => e.CoachId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExerciseCategory>().WithMany().HasForeignKey(e => e.MuscleGroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExerciseCategory>().WithMany().HasForeignKey(e => e.EquipmentId).OnDelete(DeleteBehavior.Restrict);

        // @BR-f03-thu-vien-bai-tap-020: rowversion là concurrency token của mọi lệnh sửa
        builder.Property(e => e.Version).IsRowVersion();
        builder.Ignore(e => e.DomainEvents);
    }
}
