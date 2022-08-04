using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TimeController : ControllerBase
    {
        [HttpGet("current")]
        public IActionResult GetCurrentServerTime()
        {
            return Ok(DateTimeOffset.Now);
        }
    }
}
