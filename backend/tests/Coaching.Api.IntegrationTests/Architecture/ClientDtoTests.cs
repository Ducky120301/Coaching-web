using System.Reflection;
using Coaching.Api.Controllers.Client;
using Microsoft.AspNetCore.Mvc;

namespace Coaching.Api.IntegrationTests.Architecture;

/// <summary>
/// @NFR-f02-ho-so-hoc-vien-001 @ADR-f02-ho-so-hoc-vien-007 @BR-f02-ho-so-hoc-vien-016:
/// mọi type trả về từ Controllers/Client/** không có property nào chứa "Note" (kể cả type lồng nhau).
/// </summary>
public sealed class ClientDtoTests
{
    [Fact]
    public void Client_api_dtos_never_expose_a_note()
    {
        var actions = typeof(ClientControllerBase).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ClientControllerBase)) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .ToList();
        Assert.NotEmpty(actions);

        var offenders = new List<string>();
        foreach (var action in actions)
        {
            var where = $"{action.DeclaringType!.Name}.{action.Name}";
            var produced = action.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Select(a => a.Type)
                .Where(t => t != typeof(void))
                .ToList();
            Assert.True(produced.Count > 0, $"{where} phải khai báo [ProducesResponseType<T>] để test quét được DTO.");
            foreach (var type in produced)
                Collect(type, where, offenders, []);
        }

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
            if (property.Name.Contains("Note", StringComparison.OrdinalIgnoreCase))
                offenders.Add($"{path}: {type.Name}.{property.Name}");
            Collect(property.PropertyType, $"{path} > {type.Name}.{property.Name}", offenders, seen);
        }
    }
}
