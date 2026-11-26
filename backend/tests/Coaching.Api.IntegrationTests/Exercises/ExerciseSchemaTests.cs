using Coaching.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Api.IntegrationTests.Exercises;

[Collection(ApiCollection.Name)]
public sealed class ExerciseSchemaTests(ApiFactory factory)
{
    // @ADR-f03-thu-vien-bai-tap-003 @ADR-f03-thu-vien-bai-tap-001: changeset v0.5.0 tạo đủ 2 bảng, cột và chỉ mục duy nhất
    [Fact]
    public async Task Changelog_v0_5_0_creates_exercise_library_tables()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(1, await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [DATABASECHANGELOG] WHERE [ID] = {"v0.5.0-0001-exercise-library"}")
            .SingleAsync());

        async Task<string[]> ColumnsOf(string table) => (await db.Database
            .SqlQuery<string>($"SELECT COLUMN_NAME AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = {table}")
            .ToListAsync()).Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(new[] { "CoachId", "Id", "Kind", "Name", "NameKey", "SortOrder" }, await ColumnsOf("ExerciseCategories"));
        Assert.Equal(
            new[]
            {
                "CoachId", "CommonMistakes", "DefaultRepsMax", "DefaultRepsMin", "DefaultRestSeconds", "DefaultRirMax",
                "DefaultRirMin", "DefaultSets", "EquipmentId", "Id", "IsHidden", "MuscleGroupId", "Name", "NameKey",
                "SearchKey", "TechniqueNotes", "Version", "VideoUrl",
            },
            await ColumnsOf("Exercises"));

        var unique = (await db.Database.SqlQuery<string>(
                $"SELECT [name] AS [Value] FROM sys.indexes WHERE [is_unique] = 1 AND [is_primary_key] = 0 AND [object_id] IN (OBJECT_ID(N'ExerciseCategories'), OBJECT_ID(N'Exercises'))")
            .ToListAsync()).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "IX_ExerciseCategories_CoachId_Kind_NameKey", "IX_Exercises_CoachId_NameKey_EquipmentId" }, unique);
    }
}
