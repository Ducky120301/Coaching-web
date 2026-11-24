namespace Coaching.Api.Auth;

public static class Policies
{
    /// <summary>Chỉ role Admin — áp dụng cho toàn bộ /api/admin/* (BR-15).</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Chỉ role Student — áp dụng cho toàn bộ /api/client/*.</summary>
    public const string StudentOnly = "StudentOnly";
}
