using Microsoft.Data.SqlClient;
using Reqnroll;
using Testcontainers.MsSql;

namespace Coaching.Acceptance.Support;

[Binding]
public sealed class SqlServerHooks
{
    private static MsSqlContainer? _sql;

    [BeforeTestRun]
    public static async Task StartSqlServer()
    {
        _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await _sql.StartAsync();
    }

    [AfterTestRun]
    public static async Task StopSqlServer()
    {
        if (_sql is not null)
            await _sql.DisposeAsync();
    }

    /// <summary>Mỗi scenario 1 database mới trên cùng container → scenario độc lập nhau.</summary>
    public static string NewDatabaseConnectionString() =>
        new SqlConnectionStringBuilder(_sql!.GetConnectionString())
        {
            InitialCatalog = $"acc_{Guid.NewGuid():N}",
        }.ConnectionString;
}
