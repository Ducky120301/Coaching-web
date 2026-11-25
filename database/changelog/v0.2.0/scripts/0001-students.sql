-- v0.2.0-0001: hồ sơ học viên + lịch sử trạng thái coaching
-- @ADR-f02-ho-so-hoc-vien-002: mục form trong cột JSON Registration; cột riêng chỉ cho dữ liệu cần truy vấn
-- @ADR-f02-ho-so-hoc-vien-004: Version rowversion chặn ghi đè
CREATE TABLE [Students] (
    [Id] uniqueidentifier NOT NULL,
    [CoachId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [SearchKey] nvarchar(400) NOT NULL CONSTRAINT [DF_Students_SearchKey] DEFAULT N'',
    [AccountStatus] nvarchar(20) NOT NULL,
    [CoachingStatus] nvarchar(20) NOT NULL,
    [Registration] nvarchar(max) NOT NULL CONSTRAINT [DF_Students_Registration] DEFAULT N'{}',
    [InternalNote] nvarchar(max) NULL,
    [InternalNoteUpdatedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [Version] rowversion NOT NULL,
    CONSTRAINT [PK_Students] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Students_Coaches_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [Coaches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Students_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);
GO
CREATE UNIQUE INDEX [IX_Students_Email] ON [Students] ([Email]);
GO
CREATE UNIQUE INDEX [IX_Students_UserId] ON [Students] ([UserId]);
GO
CREATE INDEX [IX_Students_CoachId_CreatedAt] ON [Students] ([CoachId], [CreatedAt]);
GO
-- @ADR-f02-ho-so-hoc-vien-005: lịch sử trạng thái coaching, chỉ thêm
CREATE TABLE [StudentCoachingStatusChanges] (
    [Id] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [FromStatus] nvarchar(20) NOT NULL,
    [ToStatus] nvarchar(20) NOT NULL,
    [ActorUserId] uniqueidentifier NOT NULL,
    [At] datetimeoffset NOT NULL,
    [Reason] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_StudentCoachingStatusChanges] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentCoachingStatusChanges_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO
CREATE INDEX [IX_StudentCoachingStatusChanges_StudentId_At] ON [StudentCoachingStatusChanges] ([StudentId], [At]);
GO
