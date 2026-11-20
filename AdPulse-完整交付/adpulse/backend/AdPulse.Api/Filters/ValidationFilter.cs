using AdPulse.Api.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AdPulse.Api.Filters;

/// <summary>
/// 全局参数校验过滤器：自动为每个 Action 参数查找对应的 FluentValidation 校验器，
/// 校验失败抛出 40000 中文提示（计划 2.3）。
/// </summary>
public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;
            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator) continue;

            var result = validator.Validate(new ValidationContext<object>(arg));
            if (!result.IsValid)
            {
                var message = string.Join("；", result.Errors.Select(e => e.ErrorMessage));
                throw ApiException.BadRequest(message);
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}