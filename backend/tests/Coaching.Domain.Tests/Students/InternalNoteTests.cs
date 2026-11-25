using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Domain.Tests.Students;

public sealed class InternalNoteTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 20, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Nov23 = new(2026, 11, 23, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly DateTimeOffset Nov24 = new(2026, 11, 24, 9, 0, 0, TimeSpan.FromHours(7));

    private static Student NewStudent() =>
        Student.Create(Guid.NewGuid(), Guid.NewGuid(), "Lan", Email.Create("lan@gmail.com"), CreatedAt);

    // @BR-f02-ho-so-hoc-vien-014
    [Fact]
    public void UpdateInternalNote_sets_note_and_its_own_time()
    {
        var student = NewStudent();

        student.UpdateInternalNote("  Tránh lunge sâu vì gối phải  ", Nov23);

        Assert.Equal("Tránh lunge sâu vì gối phải", student.InternalNote);
        Assert.Equal(Nov23, student.InternalNoteUpdatedAt);
        Assert.Null(student.UpdatedAt); // ghi chú không tính là sửa hồ sơ
    }

    // @BR-f02-ho-so-hoc-vien-014: thời điểm sửa ghi chú chỉ đổi khi ghi chú đổi
    [Fact]
    public void Same_note_keeps_previous_time()
    {
        var student = NewStudent();
        student.UpdateInternalNote("Theo dõi vai trái", Nov23);

        student.UpdateInternalNote(" Theo dõi vai trái ", Nov24);

        Assert.Equal(Nov23, student.InternalNoteUpdatedAt);
    }

    // @BR-f02-ho-so-hoc-vien-014
    [Fact]
    public void Editing_registration_does_not_touch_note_time()
    {
        var student = NewStudent();
        student.UpdateInternalNote("Theo dõi vai trái", Nov23);

        student.UpdateRegistration("Lan", RegistrationForm.CreateEmpty(), Guid.NewGuid(), Nov24);

        Assert.Equal(Nov23, student.InternalNoteUpdatedAt);
        Assert.Equal(Nov24, student.UpdatedAt);
    }

    // @BR-f02-ho-so-hoc-vien-014
    [Fact]
    public void Blank_note_clears_the_note()
    {
        var student = NewStudent();
        student.UpdateInternalNote("Theo dõi vai trái", Nov23);

        student.UpdateInternalNote("   ", Nov24);

        Assert.Null(student.InternalNote);
        Assert.Equal(Nov24, student.InternalNoteUpdatedAt);
    }

    // @ADR-f02-ho-so-hoc-vien-009: ghi chú nội bộ ≤ 5000
    [Fact]
    public void Note_longer_than_5000_is_rejected()
    {
        var student = NewStudent();
        student.UpdateInternalNote(new string('a', 5000), Nov23);

        var ex = Assert.Throws<DomainException>(() => student.UpdateInternalNote(new string('b', 5001), Nov24));

        Assert.Equal("student.internal_note_too_long", ex.Code);
        Assert.Equal(new string('a', 5000), student.InternalNote);
    }
}
