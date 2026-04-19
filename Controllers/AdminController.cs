using Microsoft.AspNetCore.Mvc;

namespace SCM_System.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Config()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }
    }
}