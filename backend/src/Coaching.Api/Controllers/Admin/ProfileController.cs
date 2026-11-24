using Coaching.Api.Common;
using Coaching.Application.Coaches;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Admin;

public sealed class ProfileController(GetMyCoachProfileHandler getProfile) : AdminControllerBase
{
    /// <summary>Hồ sơ coach của admin đang đăng nhập.</summary>
    [HttpGet]
    [ProducesResponseType<CoachProfileDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        (await getProfile.HandleAsync(cancellationToken)).ToActionResult(this);
}
