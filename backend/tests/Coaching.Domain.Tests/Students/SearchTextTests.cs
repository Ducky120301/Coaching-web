using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class SearchTextTests
{
    // @ADR-f02-ho-so-hoc-vien-003 @BR-f02-ho-so-hoc-vien-024: không phân biệt hoa/thường, không phân biệt dấu, đ = d
    [Theory]
    [InlineData("Phạm Hoàng Quân", "pham hoang quan")]
    [InlineData("Quân", "quan")]
    [InlineData("  ĐỨC ", "duc")]
    [InlineData("Hoàng Minh Đức", "hoang minh duc")]
    [InlineData("Nguyễn Thị Ánh Tuyết", "nguyen thi anh tuyet")]
    [InlineData("HQuanBilly@Gmail.com", "hquanbilly@gmail.com")]
    public void Normalize_strips_vietnamese_marks_and_lowercases(string input, string expected) =>
        Assert.Equal(expected, SearchText.Normalize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_of_blank_is_empty(string? input) => Assert.Equal(string.Empty, SearchText.Normalize(input));

    // @ADR-f02-ho-so-hoc-vien-003: SearchKey gồm họ tên + email và cập nhật khi đổi tên
    [Fact]
    public void Student_search_key_covers_name_and_email_and_follows_rename()
    {
        var at = new DateTimeOffset(2026, 11, 25, 2, 0, 0, TimeSpan.Zero);
        var student = Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Phạm Hoàng Quân", Email.Create("hquanbilly@gmail.com"), at);
        Assert.Equal("pham hoang quan hquanbilly@gmail.com", student.SearchKey);

        student.UpdateRegistration("Hoàng Minh Đức", RegistrationForm.CreateEmpty(), Guid.NewGuid(), at);

        Assert.Equal("hoang minh duc hquanbilly@gmail.com", student.SearchKey);
    }
}
