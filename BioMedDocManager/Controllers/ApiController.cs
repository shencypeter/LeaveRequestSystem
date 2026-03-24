using Microsoft.AspNetCore.Mvc;

namespace BioMedDocManager.Controllers
{
    public class ApiController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
