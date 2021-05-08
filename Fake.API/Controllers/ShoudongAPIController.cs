using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fake.API.Controllers
{
    [Route("api/shoudong")]
    public class ShoudongAPIController : Controller
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            
            return new string[] {"value1", "value2" };
        }
    }
}
