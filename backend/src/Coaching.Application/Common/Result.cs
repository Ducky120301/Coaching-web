namespace Coaching.Application.Common;

public enum ErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,

    /// <summary>Dịch vụ phụ thuộc (email, cấu hình) chưa sẵn sàng → 503.</summary>
    Unavailable,
}

/// <summary><paramref name="Extensions"/>: dữ liệu thêm vào ProblemDetails (vd. hiddenExerciseId — BR-f03-thu-vien-bai-tap-001).</summary>
public sealed record Error(
    string Code,
    string Message,
    ErrorKind Kind = ErrorKind.Validation,
    IReadOnlyDictionary<string, object?>? Extensions = null);

public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, Error? error) : base(error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Không đọc được Value của Result thất bại.");

    public static Result<T> Success(T value) => new(value, null);

    public static new Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public static implicit operator Result<T>(T value) => Success(value);
}
