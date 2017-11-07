using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiaryScraperCore
{
    public class ScrapeTaskDescriptor
    {
        public string WorkingDir { get; set; }
        [JsonIgnore]
        public Guid Guid { get; set; } = Guid.NewGuid();
        public string GuidString => this.Guid.ToString("n");
        public ScrapeTaskProgress Progress { get; set; } = new ScrapeTaskProgress();
        public string DiaryUrl { get; set; }
        public string Error { get; set; }
        public TaskStatus? Status => InnerTask?.Status;
        [JsonIgnore]
        public Task InnerTask { get; set; }
        [JsonIgnore]
        public CancellationToken Token {get;set;}
    }



    public class ScrapeTaskProgress
    {
        public string CurrentUrl { get; set; }
        public long UrlsProcessed { get; set; }
        public long PagesDownloaded { get; set; }
        public long ImagesDownloaded { get; set; }
        public long BytesDownloaded { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}