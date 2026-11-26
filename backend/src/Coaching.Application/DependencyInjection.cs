using Coaching.Application.Exercises;
using Coaching.Application.Exercises.Commands;
using Coaching.Application.Exercises.Queries;
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
        services.AddScoped<ListCategoriesHandler>(); // @BR-f03-thu-vien-bai-tap-005
        services.AddScoped<AddCategoryHandler>(); // @BR-f03-thu-vien-bai-tap-006
        services.AddScoped<CreateExerciseHandler>(); // @ADR-f03-thu-vien-bai-tap-001
        services.AddScoped<UpdateExerciseHandler>(); // @BR-f03-thu-vien-bai-tap-020
        services.AddScoped<GetExerciseHandler>();
        services.AddScoped<HideExerciseHandler>(); // @BR-f03-thu-vien-bai-tap-015
        services.AddScoped<UnhideExerciseHandler>();
        services.AddScoped<DeleteExerciseHandler>(); // @BR-f03-thu-vien-bai-tap-017
        services.AddScoped<ListExercisesHandler>(); // @BR-f03-thu-vien-bai-tap-016 @BR-f03-thu-vien-bai-tap-018 @BR-f03-thu-vien-bai-tap-019
        // @ADR-f03-thu-vien-bai-tap-004: F05 đăng ký lại IExerciseUsage bằng truy vấn giáo án (đăng ký sau thắng)
        services.AddScoped<IExerciseUsage, NoExerciseUsage>();
        return services;
    }
}
