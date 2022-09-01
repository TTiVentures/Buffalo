using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buffalo.Controllers
{
    [Route("/")]
    [AllowAnonymous]
    [ApiController]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult Status()
        {
            return Ok(new ServerStatusDto()
            {
                Via = "Buffalo",
                UtcDateTime = DateTime.UtcNow
            });
        }
    }

    internal class ServerStatusDto
    {
        public string? Via { get; set; }
        public DateTime UtcDateTime { get; set; }
    }
}