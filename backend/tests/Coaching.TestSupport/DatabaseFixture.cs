using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Coaching.TestSupport;

/// <summary>
/// @ADR-f00-codebase-011: SQL Server Testcontainers + Liquibase chạy 1 lần vào database mẫu rồi backup;
/// mỗi test/scenario restore thành 1 database mới có sẵn schema.
/// </summary>
public sealed class DatabaseFixture : IAsyncDisposable
{
    private const string TemplateDb = "coaching_template";
    private const string BackupFile = "/var/opt/mssql/data/coaching_template.bak";
    private const string LiquibaseImage = "liquibase/liquibase:4.33.0";

    private readonly INetwork _network = new NetworkBuilder().Build();
    private readonly MsSqlContainer _sql;

    public DatabaseFixture()
    {
        _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithNetwork(_network)
            .WithNetworkAliases("sql")
            .Build();
    }

    public async Task StartAsync()
    {
        await _network.CreateAsync();
        await _sql.StartAsync();
        await ExecuteAsync("master", $"CREATE DATABASE [{TemplateDb}]");
        await RunLiquibaseUpdateAsync();
        await ExecuteAsync("master", $"BACKUP DATABASE [{TemplateDb}] TO DISK = N'{BackupFile}' WITH INIT");
    }

    public async Task<string> NewDatabaseAsync()
    {
        var name = $"t_{Guid.NewGuid():N}";
        await ExecuteAsync("master",
            $"RESTORE DATABASE [{name}] FROM DISK = N'{BackupFile}' WITH " +
            $"MOVE N'{TemplateDb}' TO N'/var/opt/mssql/data/{name}.mdf', " +
            $"MOVE N'{TemplateDb}_log' TO N'/var/opt/mssql/data/{name}_log.ldf'");
        return ConnectionString(name);
    }

    public async ValueTask DisposeAsync()
    {
        await _sql.DisposeAsync();
        await _network.DisposeAsync();
    }

    private string ConnectionString(string database) =>
        new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = database }.ConnectionString;

    private async Task ExecuteAsync(string database, string sql)
    {
        await using var connection = new SqlConnection(ConnectionString(database));
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
    }

    private async Task RunLiquibaseUpdateAsync()
    {
        var liquibase = new ContainerBuilder(LiquibaseImage)
            .WithNetwork(_network)
            .WithBindMount(FindChangelogDirectory(), "/liquibase/changelog")
            .WithCommand(
                "--search-path=/liquibase/changelog",
                "--changelog-file=db.changelog-master.xml",
                $"--url=jdbc:sqlserver://sql:1433;databaseName={TemplateDb};encrypt=false;trustServerCertificate=true",
                "--username=sa",
                $"--password={MsSqlBuilder.DefaultPassword}",
                "update")
            .Build();
        await using (liquibase)
        {
            await liquibase.StartAsync();
            var exitCode = await liquibase.GetExitCodeAsync();
            if (exitCode != 0)
            {
                var (stdout, stderr) = await liquibase.GetLogsAsync();
                throw new InvalidOperationException($"Liquibase update lỗi (exit {exitCode}):\n{stdout}\n{stderr}");
            }
        }
    }

    private static string FindChangelogDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "database", "changelog");
            if (Directory.Exists(candidate))
                return candidate;
        }
        throw new DirectoryNotFoundException("Không tìm thấy thư mục database/changelog từ thư mục test.");
    }
}
