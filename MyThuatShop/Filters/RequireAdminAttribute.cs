using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyThuatShop.Filters;

public class RequireAdminAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var roleRaw = context.HttpContext.Session.GetString("Role");

        // chưa login
        if (string.IsNullOrWhiteSpace(roleRaw))
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        var role = roleRaw.Trim().ToLower();

        // chỉ cho admin
        if (role != "admin" && role != "administrator")
        {
            context.Result = new RedirectToActionResult("Index", "Home", new { });
            return;
        }

        base.OnActionExecuting(context);
    }
}
