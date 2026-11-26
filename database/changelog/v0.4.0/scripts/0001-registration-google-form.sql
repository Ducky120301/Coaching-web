-- v0.4.0-0001: form hồ sơ theo câu hỏi Google Form — chuyển Students.Registration cũ sang mục mới (F02 import Excel)
-- @ADR-f02-import-excel-002: chỉ chạm hồ sơ còn khoá cũ → chạy lại không đổi gì (@NFR-f02-import-excel-004).
-- Khoá JSON = tên property C# (EF Core ToJson). Số thập phân ghi kiểu Việt Nam (72,5). NULL trong JSON_MODIFY = không ghi khoá.
UPDATE s
SET [Registration] = JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(c.[Cleaned],
        '$.BodyFatAndMethod', v.[BodyFatAndMethod]),
        '$.WeightHeightBeforeLifting', v.[WeightHeightBeforeLifting]),
        '$.BodyPhotoUrl', v.[BodyPhotoUrl]),
    -- @BR-f02-import-excel-011: link ảnh thừa nối vào cuối ghi chú nội bộ, mỗi link một dòng
    [InternalNote] = CASE
        WHEN v.[PhotoNote] IS NULL THEN s.[InternalNote]
        WHEN NULLIF(s.[InternalNote], N'') IS NULL THEN v.[PhotoNote]
        ELSE s.[InternalNote] + NCHAR(10) + v.[PhotoNote]
    END,
    [InternalNoteUpdatedAt] = CASE WHEN v.[PhotoNote] IS NULL THEN s.[InternalNoteUpdatedAt] ELSE SYSDATETIMEOFFSET() END
FROM [Students] AS s
CROSS APPLY (SELECT
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.BodyFatPercent'))), N'') AS [Fat],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.BodyFatMethod'))), N'') AS [FatMethod],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.WeightBeforeLiftingKg'))), N'') AS [WeightThen],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.HeightThenCm'))), N'') AS [HeightThen],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.PhotoFrontUrl'))), N'') AS [Front],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.PhotoBackUrl'))), N'') AS [Back],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.PhotoLeftUrl'))), N'') AS [Left],
    NULLIF(LTRIM(RTRIM(JSON_VALUE(s.[Registration], '$.PhotoRightUrl'))), N'') AS [Right]
) AS o
CROSS APPLY (SELECT
    -- 18 → "18", 72.5 → "72,5"
    REPLACE(CONVERT(nvarchar(40), CAST(TRY_CAST(o.[Fat] AS decimal(18, 4)) AS float)), N'.', N',') + N' %' AS [FatText],
    REPLACE(CONVERT(nvarchar(40), CAST(TRY_CAST(o.[WeightThen] AS decimal(18, 4)) AS float)), N'.', N',') + N' kg' AS [WeightText],
    REPLACE(CONVERT(nvarchar(40), CAST(TRY_CAST(o.[HeightThen] AS decimal(18, 4)) AS float)), N'.', N',') + N' cm' AS [HeightText]
) AS n
CROSS APPLY (SELECT
    -- @BR-f02-import-excel-008: "<mỡ> % – <cách đo>", bỏ phần trống
    CASE WHEN n.[FatText] IS NOT NULL AND o.[FatMethod] IS NOT NULL THEN n.[FatText] + N' – ' + o.[FatMethod]
         ELSE COALESCE(n.[FatText], o.[FatMethod]) END AS [BodyFatAndMethod],
    -- @BR-f02-import-excel-009: "<cân nặng> kg, <chiều cao> cm", bỏ phần trống
    CASE WHEN n.[WeightText] IS NOT NULL AND n.[HeightText] IS NOT NULL THEN n.[WeightText] + N', ' + n.[HeightText]
         ELSE COALESCE(n.[WeightText], n.[HeightText]) END AS [WeightHeightBeforeLifting],
    -- @BR-f02-import-excel-010: link đầu tiên có giá trị theo thứ tự trước, sau, trái, phải
    COALESCE(o.[Front], o.[Back], o.[Left], o.[Right]) AS [BodyPhotoUrl],
    -- @BR-f02-import-excel-011: các link không được chọn, mỗi link một dòng ghi rõ tên ảnh
    NULLIF(CONCAT_WS(NCHAR(10),
        CASE WHEN o.[Back] IS NOT NULL AND o.[Front] IS NOT NULL
             THEN N'Ảnh mặt sau: ' + o.[Back] END,
        CASE WHEN o.[Left] IS NOT NULL AND COALESCE(o.[Front], o.[Back]) IS NOT NULL
             THEN N'Ảnh bên trái: ' + o.[Left] END,
        CASE WHEN o.[Right] IS NOT NULL AND COALESCE(o.[Front], o.[Back], o.[Left]) IS NOT NULL
             THEN N'Ảnh bên phải: ' + o.[Right] END), N'') AS [PhotoNote]
) AS v
CROSS APPLY (SELECT
    -- @BR-f02-import-excel-002: xoá Lịch rảnh, Dụng cụ; xoá các khoá đã gộp
    JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(
    JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(s.[Registration],
        '$.Availability', NULL), '$.Equipment', NULL),
        '$.BodyFatPercent', NULL), '$.BodyFatMethod', NULL),
        '$.WeightBeforeLiftingKg', NULL), '$.HeightThenCm', NULL),
        '$.PhotoFrontUrl', NULL), '$.PhotoBackUrl', NULL),
        '$.PhotoLeftUrl', NULL), '$.PhotoRightUrl', NULL) AS [Cleaned]
) AS c
WHERE EXISTS (
    SELECT 1 FROM OPENJSON(s.[Registration]) AS j
    WHERE j.[key] IN (N'Availability', N'Equipment', N'BodyFatPercent', N'BodyFatMethod', N'WeightBeforeLiftingKg',
                      N'HeightThenCm', N'PhotoFrontUrl', N'PhotoBackUrl', N'PhotoLeftUrl', N'PhotoRightUrl'));
GO
