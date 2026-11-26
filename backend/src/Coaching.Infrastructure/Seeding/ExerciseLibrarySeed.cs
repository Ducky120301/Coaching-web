namespace Coaching.Infrastructure.Seeding;

/// <summary>@ADR-f03-thu-vien-bai-tap-007: coach đã được xét nạp thư viện ban đầu (ExercisesAdded = 0 khi thư viện đã có bài).</summary>
internal sealed class ExerciseLibrarySeed(Guid coachId, DateTimeOffset seededAt, int exercisesAdded)
{
    public Guid CoachId { get; private set; } = coachId;

    public DateTimeOffset SeededAt { get; private set; } = seededAt;

    public int ExercisesAdded { get; private set; } = exercisesAdded;
}
