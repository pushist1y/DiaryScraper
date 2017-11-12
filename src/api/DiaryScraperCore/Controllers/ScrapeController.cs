using Microsoft.AspNetCore.Mvc;
using Cloudflare_Bypass;
using System.Linq;
using System;

namespace DiaryScraperCore.Controllers
{
    [Route("api/[controller]")]

    public class ScrapeController : Controller
    {
        private readonly TaskRunner _taskRunner;
        public ScrapeController(TaskRunner taskRunner)
        {
            _taskRunner = taskRunner;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Json(_taskRunner.TasksView);
        }

        [HttpPost]
        public IActionResult Post([FromBody] ScrapeTaskDescriptor descriptor)
        {
            var login = Request.GetQueryParameter("login");
            var password = Request.GetQueryParameter("password");
            _taskRunner.AddTask(descriptor, login, password);
            return Json(descriptor);
        }

        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            var task = _taskRunner.TasksView.FirstOrDefault(t => t.GuidString == id);
            if (task == null)
            {
                return NotFound();
            }
            return Json(task);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            var task = _taskRunner.RemoveTask(id);
            if (task == null)
            {
                return NotFound();
            }
            return Json(task);
        }
    }
}