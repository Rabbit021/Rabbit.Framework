using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rabbit.WebApiFramework.Core;
namespace SimplePlugin.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SimpleController : Controller
    {
        [HttpGet]
        public IActionResult GetIndex()
        {
            using (var opt = new OperationMonitor("Message"))
            {

            }
            return new ContentResult { Content = "Simple/Index" };
        }
    }
}