namespace Coaching.Infrastructure.Seeding;

/// <summary>@BR-f00-codebase-020: học viên demo chỉ khi môi trường bật (mặc định tắt, production không bao giờ bật).</summary>
public sealed class DemoSeedOptions
{
    public const string SectionName = "DemoSeed";

    public bool Enabled { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string FullName { get; set; } = "Học viên Demo";
}
