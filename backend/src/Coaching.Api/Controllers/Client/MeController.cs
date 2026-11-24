using Coaching.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Client;

public sealed record StudentMeDto(Guid UserId, string Email, string FullName);

public sealed class MeController(ICurrentUser currentUser) : ClientControllerBase
{
    /// <summary>Thông tin tài khoản học viên đang đăng nhập (hồ sơ đầy đủ bổ sung ở P1/P4).</summary>
    [HttpGet]
    [ProducesResponseType<StudentMeDto>(StatusCodes.Status200OK)]
    public IActionResult Get() =>
        Ok(new StudentMeDto(currentUser.UserId!.Value, currentUser.Email ?? string.Empty, currentUser.FullName ?? string.Empty));
}
