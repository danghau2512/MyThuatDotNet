using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyThuatShop.Extensions;
using MyThuatShop.Services;

namespace MyThuatShop.Filters;

public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.Session.GetObject<AccountApiService.UserDto>("user");
        if (user == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }
}
