using Coaching.Api.Common;
using Coaching.Application.Students;
using Coaching.Application.Students.Commands;
using Coaching.Application.Students.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Admin;

/// <summary>Hồ sơ học viên của coach đang đăng nhập. Không có endpoint xóa.</summary>
public sealed class StudentsController : AdminControllerBase
{
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
}
