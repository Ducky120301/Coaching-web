using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class RegistrationOptionsMatchTests
{
    // @BR-f02-import-excel-030 @ADR-f02-import-excel-006: chỉ khác hoa/thường, khoảng trắng, kiểu dấu gạch, dấu "/" → giá trị chuẩn
    [Theory]
    [InlineData("activityLevel", "Ít vận động (Ngồi nhiều)", "Ít vận động (ngồi nhiều)")]
    [InlineData("sleepDuration", "5-6 tiếng", "5–6 tiếng")]
    [InlineData("sleepDuration", "5 — 6 tiếng", "5–6 tiếng")]
    [InlineData("pack", "pack 1", "Pack 1")]
    [InlineData("pack", "  PACK   2 ", "Pack 2")]
    [InlineData("programStyle", "Push/Pull/Legs", "Push / Pull / Legs")]
    [InlineData("programStyle", "upper lower", "Upper / Lower")]
    [InlineData("sessionsPerWeek", "3buổi", "3 buổi")]
    public void Spelling_variant_matches_canonical_option(string field, string raw, string canonical) =>
        Assert.Equal(canonical, RegistrationOptions.Match(field, raw));

    // @BR-f02-import-excel-030: bảng cách viết đã biết (tech-design §3)
    [Theory]
    [InlineData("programStyle", "PUSH PULL LEG", "Push / Pull / Legs")]
    [InlineData("programStyle", "PUSH PULL LEGS", "Push / Pull / Legs")]
    [InlineData("programStyle", "PPL", "Push / Pull / Legs")]
    [InlineData("programStyle", "UPPER LOWER", "Upper / Lower")]
    [InlineData("programStyle", "FULLBODY", "Full body")]
    [InlineData("sleepDuration", "< 5 tiếng", "Dưới 5 tiếng")]
    [InlineData("sleepDuration", "> 8 tiếng", "Trên 8 tiếng")]
    public void Known_spelling_matches_canonical_option(string field, string raw, string canonical) =>
        Assert.Equal(canonical, RegistrationOptions.Match(field, raw));

    // @ADR-f02-import-excel-006: mọi giá trị đích trong bảng cách viết đã biết là một lựa chọn có thật
    [Fact]
    public void Known_spellings_point_to_existing_options()
    {
        foreach (var (field, spellings) in RegistrationOptions.KnownSpellings)
        {
            foreach (var canonical in spellings.Values)
                Assert.Equal(canonical, RegistrationOptions.Match(field, canonical));
        }
    }

    // @BR-f02-import-excel-030: không đưa về được → null (ô không hợp lệ)
    [Theory]
    [InlineData("stressLevel", "Cao quá")]
    [InlineData("activityLevel", "Cực kì năng động (Ví dụ: Làm những việc hoạt động chân tay nhiều)")]
    [InlineData("pack", "Pack 20 Tuần")]
    [InlineData("unknownField", "Pack 1")]
    [InlineData("pack", "   ")]
    public void Unmatched_value_returns_null(string field, string raw) =>
        Assert.Null(RegistrationOptions.Match(field, raw));

    // @ADR-f02-import-excel-006: giá trị chuẩn khớp chính nó (form nhập tay không đổi hành vi)
    [Fact]
    public void Every_option_matches_itself()
    {
        var lists = new (string Field, IReadOnlyList<string> Options)[]
        {
            ("pack", RegistrationOptions.Packs),
            ("foodBudgetReadiness", RegistrationOptions.FoodBudgetReadiness),
            ("activityLevel", RegistrationOptions.ActivityLevels),
            ("stressLevel", RegistrationOptions.StressLevels),
            ("sleepDuration", RegistrationOptions.SleepDurations),
            ("sessionsPerWeek", RegistrationOptions.SessionsPerWeek),
            ("programStyle", RegistrationOptions.ProgramStyles),
        };
        foreach (var (field, options) in lists)
        {
            foreach (var option in options)
                Assert.Equal(option, RegistrationOptions.Match(field, option));
        }
    }
}
