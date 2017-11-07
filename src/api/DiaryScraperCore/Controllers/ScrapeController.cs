using Microsoft.AspNetCore.Mvc;
using Cloudflare_Bypass;
using System.Linq;

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

            //var url = "https://thebot.net/threads/dont-panic-on-cloudflare-leaks-check-your-site-if-it-is-affected.389399/";
            // var client = CloudflareEvader.CreateBypassedWebClient(url);
            // var data = client.DownloadString(url);

            //CF_WebClient client = new CF_WebClient();
            //string html = client.DownloadString(url);
            //return Ok(html);

            return Json(_taskRunner.TasksView);
        }

        [HttpGet("new")]
        public IActionResult New()
        {
            var descriptor = new ScrapeTaskDescriptor();
            descriptor.DiaryUrl = "http://6224.diary.ru";
            descriptor.WorkingDir = "d:\\temp\\scraper";

            _taskRunner.AddTask(descriptor);

            return Json(descriptor);
        }

        [HttpPost]
        public IActionResult Post()
        {
            return Ok();
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
    }
}