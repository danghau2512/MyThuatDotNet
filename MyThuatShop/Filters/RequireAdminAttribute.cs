using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyThuatShop.Filters;

public class RequireAdminAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = (context.HttpContext.Session.GetString("Role") ?? "")
            .Trim().ToLower();

        // chỉ cho admin
        if (role != "admin" && role != "administrator")
        {
            // nếu chưa login -> đá về login
            if (string.IsNullOrWhiteSpace(role))
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { });
                return;
            }

            // có login nhưng không phải admin -> về home
            context.Result = new RedirectToActionResult("Index", "Home", new { });
            return;
        }

        base.OnActionExecuting(context);
    }
}
