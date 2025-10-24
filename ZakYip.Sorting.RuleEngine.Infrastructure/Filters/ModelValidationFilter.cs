using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Filters;

/// <summary>
/// 模型验证过滤器
/// 自动验证请求模型的有效性
/// </summary>
public class ModelValidationFilter : IActionFilter
{
    /// <summary>
    /// 在动作执行前执行
    /// </summary>
    /// <param name="context">动作执行上下文</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // 检查模型状态是否有效
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value?.Errors.Select(er => er.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            context.Result = new BadRequestObjectResult(new
            {
                error = "请求参数验证失败",
                details = errors
            });
        }
    }

    /// <summary>
    /// 在动作执行后执行
    /// </summary>
    /// <param name="context">动作已执行上下文</param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // 不需要在动作执行后做任何事情
    }
}
