
using Microsoft.AspNetCore.Mvc;

namespace DiaryScraperCore
{
    [Route("api/[controller]")]
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            var result = new [] {
                new { FirstName = "John", LastName = "Doe" },
                new { FirstName = "Mike", LastName = "Smith" }
            };

            return Json(result);
        }
    }
}