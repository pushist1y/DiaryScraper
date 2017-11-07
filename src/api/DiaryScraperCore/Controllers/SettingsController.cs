using Microsoft.AspNetCore.Mvc;

namespace DiaryScraperCore
{
    [Route("api/[controller]")]
    public class SettingsController: Controller
    {
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            return Ok();
        }
    }
}