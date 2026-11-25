using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class StudentUpdateTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 20, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EditedAt = new(2026, 11, 23, 10, 0, 0, TimeSpan.FromHours(7));
    private static readonly Guid CoachUserId = Guid.NewGuid();

    private static Student NewStudent() => Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan",
        Email.Create("lan@gmail.com"), CreatedAt, RegistrationForm.Create(new RegistrationInput { WeightKg = 72m }, Today));

    // @BR-f02-ho-so-hoc-vien-012: sửa được mọi mục kể cả số đo ban đầu; email không nằm trong lệnh sửa
    [Fact]
    public void UpdateRegistration_replaces_name_and_measurements_but_keeps_email()
    {
        var student = NewStudent();

        student.UpdateRegistration("Nguyễn Lan", RegistrationForm.Create(new RegistrationInput { WeightKg = 70m }, Today),
            CoachUserId, EditedAt);

        Assert.Equal("Nguyễn Lan", student.FullName);
        Assert.Equal(70m, student.Registration.WeightKg);
        Assert.Equal("lan@gmail.com", student.Email.Value);
    }

    // @BR-f02-ho-so-hoc-vien-013
    [Fact]
    public void UpdateRegistration_records_last_editor_and_time()
    {
        var student = NewStudent();
        Assert.Null(student.UpdatedAt);
        Assert.Null(student.UpdatedByUserId);

        student.UpdateRegistration("Lan", RegistrationForm.CreateEmpty(), CoachUserId, EditedAt);

        Assert.Equal(EditedAt, student.UpdatedAt);
        Assert.Equal(CoachUserId, student.UpdatedByUserId);
    }

    // @BR-f02-ho-so-hoc-vien-003: tên rỗng khi sửa vẫn bị từ chối, hồ sơ giữ nguyên
    [Fact]
    public void UpdateRegistration_rejects_blank_name_and_changes_nothing()
    {
        var student = NewStudent();

        var ex = Assert.Throws<DomainException>(() =>
            student.UpdateRegistration("  ", RegistrationForm.CreateEmpty(), CoachUserId, EditedAt));

        Assert.Equal("student.full_name_required", ex.Code);
        Assert.Equal("Lan", student.FullName);
        Assert.Equal(72m, student.Registration.WeightKg);
        Assert.Null(student.UpdatedAt);
    }
}
