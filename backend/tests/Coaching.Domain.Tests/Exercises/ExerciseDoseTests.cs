using Coaching.Domain.Common;
using Coaching.Domain.Exercises;

namespace Coaching.Domain.Tests.Exercises;

public sealed class ExerciseDoseTests
{
    private static string ErrorOf(Action action) => Assert.Throws<DomainException>(action).Code;

    // @ADR-f03-thu-vien-bai-tap-006: mọi phần của liều mặc định không bắt buộc
    [Fact]
    public void ADR006_Dose_without_any_value_is_empty() =>
        Assert.Equal(ExerciseDose.Empty, ExerciseDose.Create(null, null, null, null, null, null));

    // @BR-f03-thu-vien-bai-tap-009
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void BR009_Sets_from_1_to_10_are_kept(int sets) =>
        Assert.Equal(sets, ExerciseDose.Create(sets, null, null, null, null, null).Sets);

    // @BR-f03-thu-vien-bai-tap-009: 0, 11 → không lưu
    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void BR009_Sets_outside_1_to_10_are_rejected(int sets) =>
        Assert.Equal("exercise.sets_out_of_range", ErrorOf(() => ExerciseDose.Create(sets, null, null, null, null, null)));

    // @BR-f03-thu-vien-bai-tap-010: một số hoặc khoảng thấp–cao trong 1–100
    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 12)]
    [InlineData(1, 100)]
    public void BR010_Reps_number_or_range_within_1_to_100_is_kept(int min, int max)
    {
        var dose = ExerciseDose.Create(null, min, max, null, null, null);
        Assert.Equal((min, max), (dose.RepsMin!.Value, dose.RepsMax!.Value));
    }

    // @BR-f03-thu-vien-bai-tap-010 @ADR-f03-thu-vien-bai-tap-006: một số đơn lẻ lưu là thấp = cao
    [Fact]
    public void BR010_Single_reps_value_is_stored_as_min_equal_max()
    {
        var fromMin = ExerciseDose.Create(null, 10, null, null, null, null);
        var fromMax = ExerciseDose.Create(null, null, 12, null, null, null);

        Assert.Equal((10, 10), (fromMin.RepsMin!.Value, fromMin.RepsMax!.Value));
        Assert.Equal((12, 12), (fromMax.RepsMin!.Value, fromMax.RepsMax!.Value));
    }

    // @BR-f03-thu-vien-bai-tap-010: "12–10", "0", "101" → không lưu
    [Theory]
    [InlineData(12, 10)]
    [InlineData(0, 0)]
    [InlineData(101, 101)]
    [InlineData(0, 5)]
    [InlineData(5, 101)]
    public void BR010_Invalid_reps_are_rejected(int min, int max) =>
        Assert.Equal("exercise.reps_invalid", ErrorOf(() => ExerciseDose.Create(null, min, max, null, null, null)));

    // @BR-f03-thu-vien-bai-tap-011: một số hoặc khoảng thấp–cao trong 0–5
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(0, 1)]
    [InlineData(0, 5)]
    public void BR011_Rir_number_or_range_within_0_to_5_is_kept(int min, int max)
    {
        var dose = ExerciseDose.Create(null, null, null, min, max, null);
        Assert.Equal((min, max), (dose.RirMin!.Value, dose.RirMax!.Value));
    }

    // @BR-f03-thu-vien-bai-tap-011: "6", "2–1" → không lưu
    [Theory]
    [InlineData(6, 6)]
    [InlineData(2, 1)]
    [InlineData(-1, 0)]
    public void BR011_Invalid_rir_is_rejected(int min, int max) =>
        Assert.Equal("exercise.rir_invalid", ErrorOf(() => ExerciseDose.Create(null, null, null, min, max, null)));

    // @BR-f03-thu-vien-bai-tap-012: 15–600 giây
    [Theory]
    [InlineData(15)]
    [InlineData(90)]
    [InlineData(600)]
    public void BR012_Rest_from_15_to_600_seconds_is_kept(int rest) =>
        Assert.Equal(rest, ExerciseDose.Create(null, null, null, null, null, rest).RestSeconds);

    // @BR-f03-thu-vien-bai-tap-012: 10, 601 → không lưu
    [Theory]
    [InlineData(10)]
    [InlineData(14)]
    [InlineData(601)]
    public void BR012_Rest_outside_15_to_600_is_rejected(int rest) =>
        Assert.Equal("exercise.rest_out_of_range", ErrorOf(() => ExerciseDose.Create(null, null, null, null, null, rest)));
}
