using Coaching.Api.Common;
using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Commands;
using Coaching.Application.Exercises.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Admin;

/// <summary>Thư viện bài tập của coach đang đăng nhập (AdminOnly — NFR-f03-thu-vien-bai-tap-002).</summary>
public sealed class ExercisesController : AdminControllerBase
{
    // @ADR-f03-thu-vien-bai-tap-001 @BR-f03-thu-vien-bai-tap-001
    [HttpPost]
    [ProducesResponseType<ExerciseDetailDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] SaveExerciseCommand command, [FromServices] CreateExerciseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : result.Error!.ToProblem(this);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ExerciseDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, [FromServices] GetExerciseHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);

    // @BR-f03-thu-vien-bai-tap-020: bắt buộc version
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ExerciseDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveExerciseCommand command,
        [FromServices] UpdateExerciseHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, command, cancellationToken)).ToActionResult(this);
}
