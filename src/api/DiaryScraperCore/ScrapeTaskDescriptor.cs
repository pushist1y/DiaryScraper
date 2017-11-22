using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DiaryScraperCore
{
    public class ScrapeTaskDescriptor
    {
        [JsonIgnore]
        private string _error = null;
        [JsonIgnore]
        public DiaryScraperNew Scraper { get; set; }
        public string WorkingDir { get; set; }
        [JsonIgnore]
        public Guid Guid { get; set; } = Guid.NewGuid();
        public string GuidString => this.Guid.ToString("n");
        public ScrapeTaskProgress Progress => Scraper?.Progress;
        public string DiaryUrl { get; set; }
        public string Error => Progress?.Error ?? _error;
        public bool IsRunning => (InnerTask == null) || (int)InnerTask.Status < 5;
        public TaskStatus? Status => InnerTask?.Status;
        [JsonIgnore]
        public Task InnerTask => Scraper?.Worker;
        [JsonIgnore]
        public CancellationTokenSource TokenSource => Scraper?.TokenSource;
        public DateTime ScrapeStart { get; set; } = DateTime.MinValue;
        public DateTime ScrapeEnd { get; set; } = DateTime.MaxValue;
        public bool Overwrite { get; set; } = false;
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

        public void SetError(string error)
        {
            _error = error;
        }
    }


    public class ScrapeTaskProgress
    {
        public string CurrentUrl { get; set; }
        public long PagesDownloaded { get; set; }
        public long ImagesDownloaded { get; set; }
        public long BytesDownloaded { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime StartedAt { get; set; }
        public int DatePagesDiscovered { get; set; }
        public int DatePagesProcessed { get; set; }
        public string Error { get; set; }

        public void PageDownloaded(byte[] data)
        {
            BytesDownloaded += data.Length;
            PagesDownloaded += 1;
        }

        public void PageDownloaded(string html)
        {
            BytesDownloaded += System.Text.Encoding.ASCII.GetByteCount(html);
            PagesDownloaded += 1;
            LastUpdated = DateTime.Now;
        }

        public void ImageDownloaded(byte[] data)
        {
            BytesDownloaded += data.Length;
            ImagesDownloaded += 1;
            LastUpdated = DateTime.Now;
        }
    }
}