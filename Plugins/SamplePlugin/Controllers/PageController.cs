using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SimplePlugin.Controllers
{
    public class PageController : Controller
    {
        public PageController()
        {

        }
        public IActionResult Index()
        {
            return View();
        }
    }
}