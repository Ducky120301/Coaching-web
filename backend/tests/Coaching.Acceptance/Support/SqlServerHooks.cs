using Coaching.TestSupport;
using Reqnroll;

namespace Coaching.Acceptance.Support;

[Binding]
public sealed class SqlServerHooks
{
    private static DatabaseFixture? _database;

    [BeforeTestRun]
    public static async Task StartSqlServer()
    {
        _database = new DatabaseFixture();
        await _database.StartAsync();
    }

    [AfterTestRun]
    public static async Task StopSqlServer()
    {
        if (_database is not null)
            await _database.DisposeAsync();
    }

    /// <summary>Mỗi scenario 1 database mới (restore từ database mẫu đã chạy Liquibase) → scenario độc lập nhau.</summary>
    public static string NewDatabaseConnectionString() =>
        _database!.NewDatabaseAsync().GetAwaiter().GetResult();
}
