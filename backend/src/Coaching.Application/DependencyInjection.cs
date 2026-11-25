using Coaching.Application.Coaches;
using Coaching.Application.Students.Commands;
using Coaching.Application.Students.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Coaching.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký use case của tầng Application. Quy ước: mỗi use case là một handler
    /// (`XxxHandler.HandleAsync`) đăng ký Scoped, controller inject trực tiếp handler.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetMyCoachProfileHandler>();
        services.AddScoped<CurrentCoach>();
        services.AddScoped<CreateStudentHandler>();
        services.AddScoped<GetStudentHandler>();
        services.AddScoped<UpdateStudentHandler>();
        return services;
    }
}
