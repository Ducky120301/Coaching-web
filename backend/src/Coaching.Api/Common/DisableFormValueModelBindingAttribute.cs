using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Coaching.Api.Common;

/// <summary>
/// Không để model binding tự đọc form multipart trước action. Mặc định FormValueProviderFactory đọc cả form ngay khi
/// action có tham số cần bind; form vượt giới hạn thành lỗi ModelState → [ApiController] trả "request.invalid" trước khi
/// action kịp trả lỗi nghiệp vụ. Action gắn attribute này tự gọi Request.ReadFormAsync.
/// @BR-f02-import-excel-015: file &gt; 5 MB phải ra "import.file_too_large", không phải lỗi binding chung.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        context.ValueProviderFactories.RemoveType<FormValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<FormFileValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<JQueryFormValueProviderFactory>();
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
