using Coaching.Api.Common;
using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Commands;
using Coaching.Application.Exercises.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.Controllers.Admin;

/// <summary>Thư viện bài tập của coach đang đăng nhập (AdminOnly — NFR-f03-thu-vien-bai-tap-002).</summary>
public sealed class ExercisesController : AdminControllerBase
{
    // @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ExerciseListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] Guid? muscleGroupId, [FromQuery] Guid? equipmentId,
        [FromQuery] bool hidden, [FromServices] ListExercisesHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListExercisesQuery(q, muscleGroupId, equipmentId, hidden), cancellationToken)).ToActionResult(this);

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

    // @BR-f03-thu-vien-bai-tap-015
    [HttpPost("{id:guid}/hide")]
    [ProducesResponseType<ExerciseDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Hide(Guid id, [FromServices] HideExerciseHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);

    // @BR-f03-thu-vien-bai-tap-015
    [HttpPost("{id:guid}/unhide")]
    [ProducesResponseType<ExerciseDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Unhide(Guid id, [FromServices] UnhideExerciseHandler handler, CancellationToken cancellationToken) =>
        (await handler.HandleAsync(id, cancellationToken)).ToActionResult(this);

    // @BR-f03-thu-vien-bai-tap-017 @ADR-f03-thu-vien-bai-tap-004
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, [FromServices] DeleteExerciseHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : result.Error!.ToProblem(this);
    }
}
