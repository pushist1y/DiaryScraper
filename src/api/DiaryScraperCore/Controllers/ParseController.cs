using Microsoft.AspNetCore.Mvc;

namespace DiaryScraperCore
{
    [Route("api/[controller]")]
    public class ParseController : Controller
    {
        private readonly ParseTaskRunner _taskRunner;
        public ParseController(ParseTaskRunner taskRunner)
        {
            _taskRunner = taskRunner;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return Json(_taskRunner.TasksView);
        }

        [HttpPost]
        public IActionResult Post([FromBody] ParseTaskDescriptor descriptor)
        {
            return Ok();
        }

        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            return Ok();
        }

        [HttpDelete]
        public IActionResult Delete(string id)
        {
            return Ok();
        }

        [HttpGet("new")]
        public IActionResult New()
        {
            var descriptor = new ParseTaskDescriptor();
            descriptor.WorkingDir = @"d:\temp\scraper\6224\";
            _taskRunner.AddTask(descriptor);
            return Json(descriptor);
        }
    }
}