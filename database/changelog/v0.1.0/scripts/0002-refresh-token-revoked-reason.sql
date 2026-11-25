-- v0.1.0-0002: lý do thu hồi refresh token (@ADR-f00-codebase-003)
ALTER TABLE [RefreshTokens] ADD [RevokedReason] nvarchar(20) NULL;
GO
