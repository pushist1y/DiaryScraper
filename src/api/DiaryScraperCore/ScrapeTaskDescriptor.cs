using System;
using System.Collections.Generic;
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

    public abstract class TaskProgress
    {
        public abstract int Percent { get; }
        public bool RangeDiscovered { get; set; }
        [JsonIgnore]
        public string Error { get; set; }
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
        public T GetValue<T>(string name)
        {
            if (!Values.TryGetValue(name, out var val))
            {
                return default(T);
            }
            return (T)Convert.ChangeType(val, typeof(T));
        }

        public void IncrementInt(string name, long addValue)
        {
            if (Values.TryGetValue(name, out var val))
            {
                var intVal = Convert.ToInt32(val);
                Values[name] = intVal + addValue;
            }
            else
            {
                Values[name] = addValue;
            }
        }

        public void SetValue<T>(string name, T val)
        {
            Values[name] = val;
        }

    }

    public class ScrapeTaskProgress : TaskProgress
    {
        public override int Percent
        {
            get
            {
                var disc = GetValue<int>(ScrapeProgressNames.DatePagesDiscovered);
                var proc = GetValue<int>(ScrapeProgressNames.DatePagesProcessed);
                if (disc == 0)
                {
                    return RangeDiscovered ? 100 : 0;
                }
                return Convert.ToInt32(100.0 * proc / disc);
            }
        }

        public ScrapeTaskProgress()
        {
            Values[ScrapeProgressNames.CurrentUrl] = "";
            Values[ScrapeProgressNames.BytesDownloaded] = 0;
            Values[ScrapeProgressNames.PagesDownloaded] = 0;
            Values[ScrapeProgressNames.ImagesDownloaded] = 0;
            Values[ScrapeProgressNames.DatePagesDiscovered] = 0;
            Values[ScrapeProgressNames.DatePagesProcessed] = 0;
        }


        public void PageDownloaded(byte[] data)
        {
            IncrementInt(ScrapeProgressNames.BytesDownloaded, data.Length);
            IncrementInt(ScrapeProgressNames.PagesDownloaded, 1);
        }

        public void PageDownloaded(string html)
        {
            IncrementInt(ScrapeProgressNames.BytesDownloaded, System.Text.Encoding.ASCII.GetByteCount(html));
            IncrementInt(ScrapeProgressNames.PagesDownloaded, 1);
        }

        public void ImageDownloaded(byte[] data)
        {
            IncrementInt(ScrapeProgressNames.BytesDownloaded, data.Length);
            IncrementInt(ScrapeProgressNames.ImagesDownloaded, 1);
        }
    }
}