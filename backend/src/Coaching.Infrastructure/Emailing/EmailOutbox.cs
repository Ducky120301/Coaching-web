namespace Coaching.Infrastructure.Emailing;

public sealed record CapturedEmail(string To, string Subject, string TextBody, string HtmlBody, DateTimeOffset SentAt);

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-007: hộp thư trong bộ nhớ của provider Capture (singleton, thread-safe).
/// FailNext = true → lần gửi kế tiếp ném lỗi rồi tự tắt (mô phỏng dịch vụ email lỗi).
/// </summary>
public sealed class EmailOutbox
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly Lock _gate = new();
    private readonly List<CapturedEmail> _emails = [];
    private TaskCompletionSource _changed = NewSignal();
    private bool _failNext;

    public IReadOnlyList<CapturedEmail> All
    {
        get
        {
            lock (_gate)
                return _emails.ToList();
        }
    }

    public bool FailNext
    {
        get
        {
            lock (_gate)
                return _failNext;
        }
        set
        {
            lock (_gate)
                _failNext = value;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _emails.Clear();
            _failNext = false;
        }
    }

    /// <summary>Chờ tới khi có ít nhất <paramref name="count"/> thư gửi tới <paramref name="to"/>; trả thư mới nhất.</summary>
    public async Task<CapturedEmail> WaitForAsync(string to, int count = 1, TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? DefaultTimeout);
        while (true)
        {
            Task changed;
            lock (_gate)
            {
                var matches = _emails.Where(e => IsTo(e, to)).ToList();
                if (matches.Count >= count)
                    return matches[^1];
                changed = _changed.Task;
            }

            try
            {
                await changed.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Không thấy đủ {count} email gửi tới {to} trong {(timeout ?? DefaultTimeout).TotalSeconds} giây.");
            }
        }
    }

    public static bool IsTo(CapturedEmail email, string to) =>
        string.Equals(email.To, to, StringComparison.OrdinalIgnoreCase);

    internal bool TryConsumeFailure()
    {
        lock (_gate)
        {
            if (!_failNext)
                return false;
            _failNext = false;
            return true;
        }
    }

    internal void Add(CapturedEmail email)
    {
        TaskCompletionSource changed;
        lock (_gate)
        {
            _emails.Add(email);
            changed = _changed;
            _changed = NewSignal();
        }
        changed.TrySetResult();
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
