using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace SimplePlugin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SimpleController : Controller
    {
        [HttpGet]
        public IActionResult GetIndex()
        {
            return new ContentResult { Content = "Simple/Index" };
        }
    }
}