using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Rabbit.WebApiFramework.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MyTestController : ControllerBase
    {
        public IActionResult GetIndex()
        {
            return new ContentResult { Content = "MyTest WebAPI GetIndex" };
        }

        public IActionResult GetAllIndex()
        {
            return new ContentResult { Content = "MyTest WebAPI GetAllIndex" };
        }
    }
}