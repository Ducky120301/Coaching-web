using Coaching.Application.Students;
using Coaching.Domain.Exercises;

namespace Coaching.Application.Exercises;

/// <summary>Chi tiết bài trong thư viện. <see cref="Version"/>: base64 của rowversion (BR-f03-thu-vien-bai-tap-020).</summary>
public sealed record ExerciseDetailDto(
    Guid Id,
    string Name,
    Guid MuscleGroupId,
    string MuscleGroup,
    Guid EquipmentId,
    string Equipment,
    string? VideoUrl,
    bool HasVideo,
    int? Sets,
    int? RepsMin,
    int? RepsMax,
    int? RirMin,
    int? RirMax,
    int? RestSeconds,
    string? TechniqueNotes,
    string? CommonMistakes,
    bool IsHidden,
    string Version)
{
    public static ExerciseDetailDto From(Exercise e, string muscleGroup, string equipment) => new(
        e.Id, e.Name, e.MuscleGroupId, muscleGroup, e.EquipmentId, equipment, e.VideoUrl, e.HasVideo,
        e.Dose.Sets, e.Dose.RepsMin, e.Dose.RepsMax, e.Dose.RirMin, e.Dose.RirMax, e.Dose.RestSeconds,
        e.TechniqueNotes, e.CommonMistakes, e.IsHidden,
        StudentVersion.Format(e.Version)); // cùng định dạng version base64 với hồ sơ (ADR-f02-ho-so-hoc-vien-004)
}
