-- v0.3.0-0001: tài khoản học viên (F01-P1) — thời điểm gửi/hạn mật khẩu tạm/kích hoạt, lịch sử tài khoản, token kích hoạt + đặt lại
ALTER TABLE [Students] ADD
    [CredentialsSentAt] datetimeoffset NULL,
    [TempPasswordExpiresAt] datetimeoffset NULL,
    [ActivatedAt] datetimeoffset NULL;
GO
-- @ADR-f01-p1-tai-khoan-hoc-vien-006: lịch sử tài khoản là entity con của Student, chỉ thêm
CREATE TABLE [StudentAccountEvents] (
    [Id] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [Type] nvarchar(30) NOT NULL,
    [At] datetimeoffset NOT NULL,
    [ActorUserId] uniqueidentifier NULL,
    [FromEmail] nvarchar(256) NULL,
    [ToEmail] nvarchar(256) NULL,
    CONSTRAINT [PK_StudentAccountEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentAccountEvents_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO
CREATE INDEX [IX_StudentAccountEvents_StudentId_At] ON [StudentAccountEvents] ([StudentId], [At]);
GO
-- Token kích hoạt + link đặt lại: chỉ lưu SHA-256 (hex 64 ký tự). Entity EF + store ở Task 3.
CREATE TABLE [AccountTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Purpose] nvarchar(20) NOT NULL,
    [Source] nvarchar(10) NOT NULL,
    [TokenHash] nvarchar(64) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [ConsumedAt] datetimeoffset NULL,
    [RevokedAt] datetimeoffset NULL,
    CONSTRAINT [PK_AccountTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AccountTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO
CREATE UNIQUE INDEX [IX_AccountTokens_TokenHash] ON [AccountTokens] ([TokenHash]);
GO
CREATE INDEX [IX_AccountTokens_UserId_Purpose] ON [AccountTokens] ([UserId], [Purpose]);
GO
