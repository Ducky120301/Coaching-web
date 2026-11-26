using Coaching.Application.Accounts;
using Coaching.Application.Accounts.Commands;
using Coaching.Application.Coaches;
using Coaching.Application.Students;
using Coaching.Application.Students.Commands;
using Coaching.Application.Students.Import;
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
        services.AddScoped<StudentRegistrar>();
        services.AddScoped<CreateStudentHandler>();
        services.AddScoped<GetStudentHandler>();
        services.AddScoped<UpdateStudentHandler>();
        services.AddScoped<UpdateInternalNoteHandler>();
        services.AddScoped<GetStudentSelfHandler>();
        services.AddScoped<ChangeCoachingStatusHandler>();
        services.AddScoped<ListStudentsHandler>();
        services.AddScoped<CredentialsMailer>();
        services.AddScoped<SendCredentialsHandler>();
        services.AddScoped<ActivateAccountHandler>();
        services.AddScoped<ChangeStudentEmailHandler>();
        services.AddScoped<SendPasswordResetLinkHandler>();
        services.AddScoped<RequestPasswordResetHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<ImportStudentsHandler>(); // @ADR-f02-import-excel-007
        return services;
    }
}
