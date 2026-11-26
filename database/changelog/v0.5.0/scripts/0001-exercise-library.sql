-- v0.5.0-0001: thư viện bài tập (F03) — danh mục nhóm cơ/dụng cụ + bài tập
-- @ADR-f03-thu-vien-bai-tap-003: nhóm cơ và dụng cụ chung một bảng, phân biệt bằng Kind; chỉ có tên
CREATE TABLE [ExerciseCategories] (
    [Id] uniqueidentifier NOT NULL,
    [CoachId] uniqueidentifier NOT NULL,
    [Kind] nvarchar(20) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [NameKey] nvarchar(200) NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_ExerciseCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExerciseCategories_Coaches_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [Coaches] ([Id]) ON DELETE NO ACTION
);
GO
-- @BR-f03-thu-vien-bai-tap-006 @ADR-f03-thu-vien-bai-tap-002: mục cùng loại không trùng khoá tên (chốt chặn cuối khi hai request đua)
CREATE UNIQUE INDEX [IX_ExerciseCategories_CoachId_Kind_NameKey] ON [ExerciseCategories] ([CoachId], [Kind], [NameKey]);
GO
-- @ADR-f03-thu-vien-bai-tap-001: bài tập; liều mặc định là các cột int NULL (@ADR-f03-thu-vien-bai-tap-006); Version chặn ghi đè
CREATE TABLE [Exercises] (
    [Id] uniqueidentifier NOT NULL,
    [CoachId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [NameKey] nvarchar(200) NOT NULL,
    [SearchKey] nvarchar(200) NOT NULL,
    [MuscleGroupId] uniqueidentifier NOT NULL,
    [EquipmentId] uniqueidentifier NOT NULL,
    [VideoUrl] nvarchar(2048) NULL,
    [DefaultSets] int NULL,
    [DefaultRepsMin] int NULL,
    [DefaultRepsMax] int NULL,
    [DefaultRirMin] int NULL,
    [DefaultRirMax] int NULL,
    [DefaultRestSeconds] int NULL,
    [TechniqueNotes] nvarchar(2000) NULL,
    [CommonMistakes] nvarchar(2000) NULL,
    [IsHidden] bit NOT NULL CONSTRAINT [DF_Exercises_IsHidden] DEFAULT 0,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_Exercises] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Exercises_Coaches_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [Coaches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Exercises_ExerciseCategories_MuscleGroupId] FOREIGN KEY ([MuscleGroupId]) REFERENCES [ExerciseCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Exercises_ExerciseCategories_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [ExerciseCategories] ([Id]) ON DELETE NO ACTION
);
GO
-- @BR-f03-thu-vien-bai-tap-001 @ADR-f03-thu-vien-bai-tap-001: duy nhất theo (tên chuẩn hoá, dụng cụ) trong thư viện của coach, kể cả bài đang ẩn
CREATE UNIQUE INDEX [IX_Exercises_CoachId_NameKey_EquipmentId] ON [Exercises] ([CoachId], [NameKey], [EquipmentId]);
GO
-- Danh sách bài: 1 truy vấn theo (CoachId, IsHidden) + LIKE trên SearchKey (NFR-f03-thu-vien-bai-tap-001)
CREATE INDEX [IX_Exercises_CoachId_IsHidden] ON [Exercises] ([CoachId], [IsHidden]);
GO
CREATE INDEX [IX_Exercises_MuscleGroupId] ON [Exercises] ([MuscleGroupId]);
GO
CREATE INDEX [IX_Exercises_EquipmentId] ON [Exercises] ([EquipmentId]);
GO
