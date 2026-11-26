using Coaching.Api.Common;
using Coaching.Application.Students;
using Coaching.Application.Students.Commands;
using Coaching.Application.Students.Import;
using Coaching.Application.Students.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Admin;

/// <summary>Hồ sơ học viên của coach đang đăng nhập. Không có endpoint xóa.</summary>
public sealed class StudentsController : AdminControllerBase
{
    /// <summary>Phần multipart ngoài nội dung file (ranh giới, tiêu đề phần).</summary>
    private const long ImportOverheadBytes = 64 * 1024;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StudentListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? q, [FromServices] ListStudentsHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(q, cancellationToken)).ToActionResult(this);

    [HttpPost]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateStudentCommand command,
        [FromServices] CreateStudentHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : result.Error!.ToProblem(this);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, [FromServices] GetStudentHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPut("{id:guid}")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentCommand command,
        [FromServices] UpdateStudentHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, command, cancellationToken)).ToActionResult(this);

    [HttpPut("{id:guid}/internal-note")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateInternalNote(Guid id, [FromBody] UpdateInternalNoteCommand command,
        [FromServices] UpdateInternalNoteHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, command, cancellationToken)).ToActionResult(this);

    // @BR-f02-ho-so-hoc-vien-021: route này chỉ có POST — PUT/DELETE → 405, không sửa/xóa được lịch sử
    [HttpPost("{id:guid}/coaching-status")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeCoachingStatus(Guid id, [FromBody] ChangeCoachingStatusCommand command,
        [FromServices] ChangeCoachingStatusHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, command, cancellationToken)).ToActionResult(this);

    // @BR-f01-p1-tai-khoan-hoc-vien-025: trả StudentDetailDto — mật khẩu tạm chỉ có trong email
    [HttpPost("{id:guid}/credentials")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendCredentials(Guid id, [FromServices] SendCredentialsHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);

    // @BR-f01-p1-tai-khoan-hoc-vien-007: sửa email chỉ khi Chờ kích hoạt; route riêng, bắt buộc version như mọi lệnh sửa
    [HttpPut("{id:guid}/email")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeEmail(Guid id, [FromBody] ChangeStudentEmailCommand command,
        [FromServices] ChangeStudentEmailHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, command, cancellationToken)).ToActionResult(this);

    // @BR-f01-p1-tai-khoan-hoc-vien-017: chỉ tài khoản Đã kích hoạt, mọi trạng thái coaching; link chỉ có trong email
    [HttpPost("{id:guid}/password-reset-link")]
    [ProducesResponseType<StudentDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendPasswordResetLink(Guid id, [FromServices] SendPasswordResetLinkHandler handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);

    // @BR-f02-import-excel-012 @BR-f02-import-excel-013: AdminOnly như cả controller; hồ sơ gắn coach đang đăng nhập.
    // @ADR-f02-import-excel-007: xử lý đồng bộ; file giữ trong bộ nhớ (MemoryBufferThreshold ≥ giới hạn) — không ghi đĩa.
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [DisableFormValueModelBinding]
    [RequestSizeLimit(ImportStudentsHandler.MaxBytes + ImportOverheadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImportStudentsHandler.MaxBytes + ImportOverheadBytes,
        MemoryBufferThreshold = (int)(ImportStudentsHandler.MaxBytes + ImportOverheadBytes))]
    [ProducesResponseType<ImportResultDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Import([FromServices] ImportStudentsHandler handler,
        [FromServices] ILogger<StudentsController> logger, CancellationToken cancellationToken)
    {
        // @BR-f02-import-excel-015: từ chối theo Content-Length trước khi đọc body (không để 413 thô của Kestrel)
        if (Request.ContentLength > ImportStudentsHandler.MaxBytes + ImportOverheadBytes)
            return ImportErrors.FileTooLarge.ToProblem(this);

        IFormFile? file;
        try
        {
            file = (await Request.ReadFormAsync(cancellationToken)).Files.GetFile("file");
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return ImportErrors.FileTooLarge.ToProblem(this);
        }
        catch (InvalidDataException)
        {
            return ImportErrors.FileInvalid.ToProblem(this);
        }

        if (file is null)
            return ImportErrors.FileInvalid.ToProblem(this); // @BR-f02-import-excel-014

        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(stream, file.Length, cancellationToken);
        if (result.IsSuccess) // Monitoring: chỉ số dòng, không log nội dung ô
            logger.LogInformation("Import hồ sơ học viên: tạo {Created}, bỏ qua {Skipped}", result.Value.Created, result.Value.Skipped);
        return result.ToActionResult(this);
    }
}
