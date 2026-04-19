using Microsoft.AspNetCore.Mvc;

namespace SCM_System.Controllers
{
    public class StorageController : Controller
    {
        
        public IActionResult Category()
        {
            return View();
        }
    }
}