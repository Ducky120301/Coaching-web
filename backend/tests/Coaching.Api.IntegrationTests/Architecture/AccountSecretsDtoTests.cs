using System.Reflection;
using Coaching.Api.Controllers.Admin;
using Coaching.Api.Controllers.Client;
using Coaching.Application.Students;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.IntegrationTests.Architecture;

/// <summary>
/// @BR-f01-p1-tai-khoan-hoc-vien-025: mọi type trả về từ /api/admin/** và /api/client/** không có property chuỗi nào mang
/// mật khẩu hay mã bí mật (tên chứa Password / Token / Secret), kể cả type lồng nhau. Mốc thời gian như TempPasswordExpiresAt được phép.
/// </summary>
public sealed class AccountSecretsDtoTests
{
    private static readonly string[] SecretWords = ["Password", "Token", "Secret"];

    [Fact]
    public void Admin_and_client_dtos_never_carry_a_password_or_token()
    {
        var produced = typeof(Program).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(AdminControllerBase)) || t.IsSubclassOf(typeof(ClientControllerBase))))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .SelectMany(a => a.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Where(p => p.Type != typeof(void))
                .Select(p => (Where: $"{a.DeclaringType!.Name}.{a.Name}", p.Type)))
            .ToList();
        Assert.Contains(produced, p => p.Type == typeof(StudentDetailDto));

        var offenders = new List<string>();
        foreach (var (where, type) in produced)
            Collect(type, where, offenders, []);

        Assert.Empty(offenders);
    }

    private static void Collect(Type type, string path, List<string> offenders, HashSet<Type> seen)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string) || type.IsPrimitive || type.IsEnum || !seen.Add(type))
            return;
        if (type.IsArray)
        {
            Collect(type.GetElementType()!, path, offenders, seen);
            return;
        }
        if (type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true)
        {
            foreach (var argument in type.GetGenericArguments())
                Collect(argument, path, offenders, seen);
            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType == typeof(string)
                && SecretWords.Any(w => property.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
                offenders.Add($"{path}: {type.Name}.{property.Name}");
            Collect(property.PropertyType, $"{path} > {type.Name}.{property.Name}", offenders, seen);
        }
    }
}
