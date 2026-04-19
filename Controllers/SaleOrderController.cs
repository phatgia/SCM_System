using Microsoft.AspNetCore.Mvc;

namespace SCM_System.Controllers
{
    public class SalesController : Controller
    {
        public IActionResult Create ()
        {
            return View();
        }
        
        public IActionResult Orders ()
        {
            return View();
        }

        public IActionResult Stock ()
        {
            return View();
        }

        public IActionResult Returns ()
        {
            return View();
        }
    }
}
