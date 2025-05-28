using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class IdSanitizationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var arg in context.ActionArguments)
        {
            if (
                arg.Key.Contains("id", StringComparison.CurrentCultureIgnoreCase)
                && arg.Value is string idValue
            )
            {
                if (
                    string.IsNullOrWhiteSpace(idValue) || idValue.Any(c => !char.IsLetterOrDigit(c))
                )
                {
                    context.Result = new BadRequestObjectResult("Invalid ID format.");
                    return;
                }
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
