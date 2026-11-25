using Coaching.Infrastructure.Emailing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers;

public sealed record TestEmailDto(string To, string Subject, string TextBody, DateTimeOffset SentAt);

/// <summary>
/// @ADR-f01-p1-tai-khoan-hoc-vien-007: e2e đọc thư của provider Capture. Chỉ được map khi Email:Provider=Capture
/// (Program.cs loại controller này ở mọi provider khác); Capture bị cấm ở Production.
/// </summary>
[ApiController]
[Route("api/test/emails")]
[AllowAnonymous]
public sealed class TestEmailsController(EmailOutbox outbox) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TestEmailDto>>(StatusCodes.Status200OK)]
    public IActionResult List([FromQuery] string? to) =>
        Ok(outbox.All
            .Where(e => to is null || EmailOutbox.IsTo(e, to))
            .Select(e => new TestEmailDto(e.To, e.Subject, e.TextBody, e.SentAt))
            .ToList());
}
