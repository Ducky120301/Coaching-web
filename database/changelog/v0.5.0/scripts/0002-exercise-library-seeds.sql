-- v0.5.0-0002: đánh dấu coach đã được nạp thư viện bài tập ban đầu
-- @ADR-f03-thu-vien-bai-tap-007: mỗi coach chỉ nạp một lần — coach đã sửa/xoá bài không bao giờ bị nạp lại hay nhân đôi
CREATE TABLE [ExerciseLibrarySeeds] (
    [CoachId] uniqueidentifier NOT NULL,
    [SeededAt] datetimeoffset NOT NULL,
    [ExercisesAdded] int NOT NULL,
    CONSTRAINT [PK_ExerciseLibrarySeeds] PRIMARY KEY ([CoachId]),
    CONSTRAINT [FK_ExerciseLibrarySeeds_Coaches_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [Coaches] ([Id]) ON DELETE NO ACTION
);
GO
