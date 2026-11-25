using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Coaching.Api.Common;

/// <summary>Bỏ controller khỏi MVC (route không tồn tại → 404). Chạy sau ControllerFeatureProvider mặc định.</summary>
internal sealed class ExcludeControllersFeatureProvider(params Type[] excluded) : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var type in excluded)
            feature.Controllers.Remove(type.GetTypeInfo());
    }
}
