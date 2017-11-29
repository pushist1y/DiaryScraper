using System.Linq;
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
           if (_taskRunner.TasksView.Any(t => t.IsRunning))
            {
                descriptor.SetError("Операция по скачиванию дневника уже выполняется");
                return Json(descriptor);
            }
            _taskRunner.AddTask(descriptor);
            return Json(descriptor);
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