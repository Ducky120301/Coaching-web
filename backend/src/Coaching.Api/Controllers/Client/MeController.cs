using Coaching.Api.Common;
using Coaching.Application.Students.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Client;

public sealed class MeController(GetStudentSelfHandler getSelf) : ClientControllerBase
{
    /// <summary>
    /// Hồ sơ của học viên đang đăng nhập.
    /// @ADR-f02-ho-so-hoc-vien-007 @BR-f02-ho-so-hoc-vien-016: DTO riêng phía học viên, không có ghi chú nội bộ.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<StudentSelfDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        (await getSelf.HandleAsync(cancellationToken)).ToActionResult(this);
}
