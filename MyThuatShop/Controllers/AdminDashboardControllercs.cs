using Microsoft.AspNetCore.Mvc;

namespace MyThuatShop.Controllers
{
    [Route("admin")]
    public class AdminDashboardController : Controller
    {
        private bool IsAdmin()
            => string.Equals(HttpContext.Session.GetString("Role"), "Admin",
                StringComparison.OrdinalIgnoreCase);

        [HttpGet("")]
        public IActionResult Index()
        {
            if (!IsAdmin()) return Redirect("/login");
            return Redirect("/admin/overview");
        }

    }
}
