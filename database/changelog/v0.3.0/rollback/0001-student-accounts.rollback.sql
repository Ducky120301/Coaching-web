-- Rollback v0.3.0-0001: xóa 2 bảng rồi 3 cột
DROP TABLE IF EXISTS [AccountTokens];
GO
DROP TABLE IF EXISTS [StudentAccountEvents];
GO
ALTER TABLE [Students] DROP COLUMN [ActivatedAt], [TempPasswordExpiresAt], [CredentialsSentAt];
GO
