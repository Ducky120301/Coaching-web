-- Rollback v0.1.0-0001: xóa bảng theo thứ tự ngược khóa ngoại
DROP TABLE IF EXISTS [AuditLogs];
GO
DROP TABLE IF EXISTS [RefreshTokens];
GO
DROP TABLE IF EXISTS [Coaches];
GO
DROP TABLE IF EXISTS [AspNetUserTokens];
GO
DROP TABLE IF EXISTS [AspNetUserRoles];
GO
DROP TABLE IF EXISTS [AspNetUserLogins];
GO
DROP TABLE IF EXISTS [AspNetUserClaims];
GO
DROP TABLE IF EXISTS [AspNetRoleClaims];
GO
DROP TABLE IF EXISTS [AspNetUsers];
GO
DROP TABLE IF EXISTS [AspNetRoles];
GO
