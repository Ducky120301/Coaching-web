using Coaching.Domain.Common;

namespace Coaching.Domain.Exercises;

/// <summary>
/// @ADR-f03-thu-vien-bai-tap-006: liều mặc định có cấu trúc (Sets, Reps thấp–cao, RIR thấp–cao, nghỉ giây); mọi phần không
/// bắt buộc. Một số đơn lẻ lưu là thấp = cao (chỉ gửi một phía → gộp về phía có). F05 dùng lại số khi soạn giáo án.
/// </summary>
public sealed record ExerciseDose(int? Sets, int? RepsMin, int? RepsMax, int? RirMin, int? RirMax, int? RestSeconds)
{
    public const int SetsMin = 1;
    public const int SetsMax = 10;
    public const int RepsLowest = 1;
    public const int RepsHighest = 100;
    public const int RirLowest = 0;
    public const int RirHighest = 5;
    public const int RestMinSeconds = 15;
    public const int RestMaxSeconds = 600;

    public static readonly ExerciseDose Empty = new(null, null, null, null, null, null);

    public static ExerciseDose Create(int? sets, int? repsMin, int? repsMax, int? rirMin, int? rirMax, int? restSeconds)
    {
        // @BR-f03-thu-vien-bai-tap-009
        if (sets is < SetsMin or > SetsMax)
            throw new DomainException("exercise.sets_out_of_range", "Sets phải từ 1 đến 10.");

        // @BR-f03-thu-vien-bai-tap-010
        var (repsLow, repsHigh) = Range(repsMin, repsMax, RepsLowest, RepsHighest, "exercise.reps_invalid",
            "Reps phải là một số hoặc khoảng thấp–cao trong 1–100, số thấp không lớn hơn số cao.");

        // @BR-f03-thu-vien-bai-tap-011
        var (rirLow, rirHigh) = Range(rirMin, rirMax, RirLowest, RirHighest, "exercise.rir_invalid",
            "RIR phải là một số hoặc khoảng thấp–cao trong 0–5, số thấp không lớn hơn số cao.");

        // @BR-f03-thu-vien-bai-tap-012
        if (restSeconds is < RestMinSeconds or > RestMaxSeconds)
            throw new DomainException("exercise.rest_out_of_range", "Thời gian nghỉ phải từ 15 đến 600 giây.");

        return new ExerciseDose(sets, repsLow, repsHigh, rirLow, rirHigh, restSeconds);
    }

    private static (int? Low, int? High) Range(int? min, int? max, int lowest, int highest, string code, string message)
    {
        if (min is null && max is null)
            return (null, null);

        var low = min ?? max!.Value;
        var high = max ?? min!.Value;
        if (low < lowest || high > highest || low > high)
            throw new DomainException(code, message);
        return (low, high);
    }
}
