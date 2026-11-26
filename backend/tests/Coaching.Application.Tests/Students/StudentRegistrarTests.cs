using Coaching.Application.Abstractions;
using Coaching.Application.Common;
using Coaching.Application.Students;
using Coaching.Domain.Common;
using Coaching.Domain.Students;

namespace Coaching.Application.Tests.Students;

public sealed class StudentRegistrarTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 2, 0, 0, TimeSpan.Zero);

    private readonly FakeStudents _students = new();
    private readonly FakeAccounts _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private StudentRegistrar Registrar() => new(_students, _accounts, _unitOfWork, new FixedClock(Now));

    // @BR-f02-ho-so-hoc-vien-005 @BR-f02-ho-so-hoc-vien-006 @BR-f02-ho-so-hoc-vien-007 @BR-f02-ho-so-hoc-vien-014
    // @ADR-f02-ho-so-hoc-vien-001: hồ sơ + tài khoản Chờ kích hoạt trong một transaction
    [Fact]
    public async Task Registers_pending_active_student_with_pending_account_in_one_transaction()
    {
        var coachId = Guid.NewGuid();
        var email = Email.Create("lan@gmail.com");

        var result = await Registrar().RegisterAsync(coachId, " Lan ", email, RegistrationForm.CreateEmpty(), "Hay đau gối");

        Assert.True(result.IsSuccess);
        var student = result.Value;
        Assert.Equal(coachId, student.CoachId);
        Assert.Equal("Lan", student.FullName);
        Assert.Equal(email, student.Email);
        Assert.Equal(AccountStatus.PendingActivation, student.AccountStatus);
        Assert.Equal(CoachingStatus.Active, student.CoachingStatus);
        Assert.Equal(Now, student.CreatedAt);
        Assert.Equal("Hay đau gối", student.InternalNote);
        Assert.Equal(Now, student.InternalNoteUpdatedAt);
        Assert.Equal(new[] { (student.UserId, email, "Lan") }, _accounts.Created);
        Assert.Same(student, Assert.Single(_students.Added));
        Assert.Equal(1, _unitOfWork.Transactions);
        Assert.Equal(1, _unitOfWork.Saves);
    }

    // @BR-f02-ho-so-hoc-vien-001: email đã có tài khoản → lỗi, không thêm hồ sơ
    [Fact]
    public async Task Email_taken_returns_error_and_adds_nothing()
    {
        _accounts.Outcome = Result.Failure(StudentErrors.EmailTaken);

        var result = await Registrar().RegisterAsync(Guid.NewGuid(), "Lan", Email.Create("lan@gmail.com"),
            RegistrationForm.CreateEmpty(), null);

        Assert.Equal(StudentErrors.EmailTaken, result.Error);
        Assert.Empty(_students.Added);
        Assert.Equal(0, _unitOfWork.Saves);
    }

    // @BR-f02-ho-so-hoc-vien-003: họ tên trống → DomainException trước khi tạo tài khoản
    [Fact]
    public async Task Invalid_full_name_throws_before_creating_account()
    {
        await Assert.ThrowsAsync<DomainException>(() => Registrar().RegisterAsync(Guid.NewGuid(), "   ",
            Email.Create("lan@gmail.com"), RegistrationForm.CreateEmpty(), null));

        Assert.Empty(_accounts.Created);
        Assert.Equal(0, _unitOfWork.Transactions);
    }

    // @BR-f02-ho-so-hoc-vien-014: không có ghi chú → không có thời điểm sửa ghi chú
    [Fact]
    public async Task No_note_leaves_note_timestamp_empty()
    {
        var result = await Registrar().RegisterAsync(Guid.NewGuid(), "Lan", Email.Create("lan@gmail.com"),
            RegistrationForm.CreateEmpty(), "  ");

        Assert.Null(result.Value.InternalNote);
        Assert.Null(result.Value.InternalNoteUpdatedAt);
    }

    private sealed class FakeStudents : IStudentRepository
    {
        public List<Student> Added { get; } = [];

        public void Add(Student student) => Added.Add(student);

        public Task<Student?> GetAsync(Guid id, Guid coachId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Student?> GetForUpdateAsync(Guid id, Guid coachId, byte[] expectedVersion,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Student?> GetTrackedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StudentSummary>> ListAsync(Guid coachId, string normalizedQuery,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeAccounts : IStudentAccounts
    {
        public Result Outcome { get; set; } = Result.Success();

        public List<(Guid UserId, Email Email, string FullName)> Created { get; } = [];

        public Task<Result> CreatePendingAsync(Guid userId, Email email, string fullName,
            CancellationToken cancellationToken = default)
        {
            Created.Add((userId, email, fullName));
            return Task.FromResult(Outcome);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int Transactions { get; private set; }

        public int Saves { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Saves++;
            return Task.FromResult(1);
        }

        public Task<Result<T>> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>> operation,
            CancellationToken cancellationToken = default)
        {
            Transactions++;
            return operation(cancellationToken);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        private static readonly TimeSpan Vietnam = TimeSpan.FromHours(7);

        public DateTimeOffset UtcNow => now;

        public DateTimeOffset VietnamNow => now.ToOffset(Vietnam);

        public DateOnly Today => DateOnly.FromDateTime(VietnamNow.DateTime);

        public DateTimeOffset ToVietnamTime(DateTimeOffset instant) => instant.ToOffset(Vietnam);
    }
}
