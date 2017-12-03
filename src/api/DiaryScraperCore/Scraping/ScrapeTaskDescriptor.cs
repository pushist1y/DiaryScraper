using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DiaryScraperCore
{
    public class ScrapeTaskDescriptor : TaskDescriptorBase
    {

        [JsonIgnore]
        public DiaryScraperNew Scraper { get; set; }

        public ScrapeTaskProgress Progress => Scraper?.Progress;
        public string DiaryUrl { get; set; }
        public override string Error => Progress?.Error ?? _error;

        [JsonIgnore]
        public override Task InnerTask => Scraper?.Worker;
        [JsonIgnore]
        public override CancellationTokenSource TokenSource => Scraper?.TokenSource;
        public DateTime ScrapeStart { get; set; } = DateTime.MinValue;
        public DateTime ScrapeEnd { get; set; } = DateTime.MaxValue;
        public bool Overwrite { get; set; } = false;
        public bool DownloadEdits { get; set; } = false;
        public bool DownloadAccount { get; set; } = false;
        private int _requestDelay = 1000;
        public int RequestDelay
        {
            get { return _requestDelay; }
            set
            {
                if (value < 100 || value > 10000)
                {
                    _requestDelay = 1000;
                }
                else
                {
                    _requestDelay = value;
                }
            }
        }
    }
}