using Microsoft.AspNetCore.Mvc;
using Cloudflare_Bypass;

namespace DiaryScraperCore.Controllers
{
    [Route("api/[controller]")]
    
    public class ScrapController: Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var url = "https://thebot.net/threads/dont-panic-on-cloudflare-leaks-check-your-site-if-it-is-affected.389399/";
            // var client = CloudflareEvader.CreateBypassedWebClient(url);
            // var data = client.DownloadString(url);

            CF_WebClient client = new CF_WebClient();
            string html = client.DownloadString(url);
            return Ok(html);
        }
    }
}