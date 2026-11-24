using Coaching.Application.Abstractions;
using Coaching.Application.Coaches;
using Coaching.Application.Common;
using Coaching.Domain.Coaches;
using Coaching.Domain.Common;

namespace Coaching.Application.Tests.Coaches;

public sealed class GetMyCoachProfileHandlerTests
{
    [Fact]
    public async Task Returns_profile_of_logged_in_coach()
    {
        var userId = Guid.NewGuid();
        var coach = Coach.Create(userId, "Coach Duobexiuuu", Email.Create("coach@duobexiuuu.vn"));
        var handler = new GetMyCoachProfileHandler(new FakeCoaches(coach), new FakeCurrentUser(userId));

        var result = await handler.HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(new CoachProfileDto(coach.Id, "Coach Duobexiuuu", "coach@duobexiuuu.vn"), result.Value);
    }

    [Fact]
    public async Task Returns_not_found_when_account_has_no_coach_profile()
    {
        var handler = new GetMyCoachProfileHandler(new FakeCoaches(), new FakeCurrentUser(Guid.NewGuid()));

        var result = await handler.HandleAsync();

        Assert.Equal(GetMyCoachProfileHandler.NotFound, result.Error);
    }

    [Fact]
    public async Task Returns_unauthorized_when_not_logged_in()
    {
        var handler = new GetMyCoachProfileHandler(new FakeCoaches(), new FakeCurrentUser(null));

        var result = await handler.HandleAsync();

        Assert.Equal(ErrorKind.Unauthorized, result.Error!.Kind);
    }

    private sealed class FakeCoaches(params Coach[] coaches) : ICoachRepository
    {
        private readonly List<Coach> _coaches = [.. coaches];

        public Task<Coach?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_coaches.SingleOrDefault(c => c.UserId == userId));

        public void Add(Coach coach) => _coaches.Add(coach);
    }

    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUser
    {
        public bool IsAuthenticated => userId is not null;
        public Guid? UserId => userId;
        public string? Email => null;
        public string? FullName => null;
        public bool IsInRole(string role) => false;
    }
}
