using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cloudflare_Bypass;

namespace DiaryScraperCore
{
    public class DiaryScraper
    {
        private string _login;
        private string _pass;
        private ScrapeTaskDescriptor _descriptor;
        private readonly CF_WebClient _webClient;
        private string _diaryBaseUrl;
        private string _diaryName;
        public DiaryScraper(ScrapeTaskDescriptor descriptor) : this(descriptor, null, null)
        {

        }

        public DiaryScraper(ScrapeTaskDescriptor descriptor, string login, string pass)
        {
            _login = login;
            _pass = pass;
            _descriptor = descriptor;
            _webClient = new CF_WebClient();
        }

        public Task Run()
        {
            var src = new CancellationTokenSource();
            _descriptor.Progress.StartedAt = DateTime.Now;
            _descriptor.Token = src.Token;
            _descriptor.InnerTask = new Task(() => DoWork(_descriptor.Token));
            _descriptor.InnerTask.Start();
            return _descriptor.InnerTask;
        }

        private bool CheckDiaryUrl()
        {
            var match = Regex.Match(_descriptor.DiaryUrl, @"([\w-]+)\.diary\.ru");
            if (!match.Success)
            {
                return false;
            }
            _diaryBaseUrl = $"http://{match.Groups[1].Value}.diary.ru";
            _diaryName = match.Groups[1].Value;
            return true;


        }

        private List<DateUrlInfo> _dateUrls = new List<DateUrlInfo>();
        private bool FillDateUrls(CancellationToken cancellationToken)
        {
            var calendarUrl = _diaryBaseUrl + "/?calendar";
            var html = _webClient.DownloadString(calendarUrl);
            _descriptor.Progress.BytesDownloaded += html.Length;
            _descriptor.Progress.PagesDownloaded += 1;
            var matches = Regex.Matches(html, @"calendar&year=(\d+)");
            if (matches.Count <= 0)
            {
                return false;
            }
            var yearUrls = new List<string>();
            foreach (Match m in matches)
            {
                var strYear = m.Groups[1].Value;
                var yearStart = new DateTime(Convert.ToInt32(strYear), 1, 1);
                var yearEnd = new DateTime(Convert.ToInt32(strYear), 12, 31);
                if (yearStart <= _descriptor.ScrapeEnd && yearEnd >= _descriptor.ScrapeStart)
                {
                    yearUrls.Add(_diaryBaseUrl + $"/?calendar&year={strYear}");
                }
            }

            foreach (var yearUrl in yearUrls)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
                Thread.Sleep(_descriptor.RequestDelay); // delay web requests
                html = _webClient.DownloadString(yearUrl);
                _descriptor.Progress.BytesDownloaded += html.Length;
                _descriptor.Progress.PagesDownloaded += 1;

                matches = Regex.Matches(html, @"diary.ru(\/\?date=(\d+)-(\d+)-(\d+))");
                foreach (Match m in matches)
                {
                    var date = new DateTime(Convert.ToInt32(m.Groups[2].Value),
                                            Convert.ToInt32(m.Groups[3].Value),
                                            Convert.ToInt32(m.Groups[4].Value));
                    if (date < _descriptor.ScrapeStart || date > _descriptor.ScrapeEnd)
                    {
                        continue;
                    }

                    _dateUrls.Add(new DateUrlInfo { Url = _diaryBaseUrl + m.Groups[1].Value, PostDate = date });
                }
            }

            return true;
        }

        private void DoWork(CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;

            if (_descriptor.ScrapeStart > _descriptor.ScrapeEnd)
            {
                _descriptor.Error = "Scrape interval provided is wrong";
                return;
            }

            

            if (!CheckDiaryUrl())
            {
                _descriptor.Error = "Diary url is wrong";
                return;
            }

            EnsureDirs();

            if (!string.IsNullOrEmpty(_login) && !string.IsNullOrEmpty(_pass))
            {
                Login();
            }

            if (!FillDateUrls(cancellationToken))
            {
                _descriptor.Error = "Error checking calendar (probably not enough permissions)";
                return;
            }

            _descriptor.Progress.DatePagesDiscovered = _dateUrls.Count;

            if (!ScanDateUrls(cancellationToken))
            {
                _descriptor.Error = "Error checking calendar (probably not enough permissions)";
                return;
            }

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _descriptor.Error = "Process was cancelled by user";
                    return;
                }
                if (DateTime.Now - startTime > TimeSpan.FromMinutes(2))
                {
                    return;
                }

                Thread.Sleep(_descriptor.RequestDelay);
                _descriptor.Progress.LastUpdated = DateTime.Now;
                _descriptor.Progress.BytesDownloaded += 1024;
            }
        }

        private bool ScanDateUrls(CancellationToken cancellationToken)
        {
            var pattern = $@"({_diaryName}\.diary\.ru\/p\w+\.htm).{{0,30}}URL";
            foreach (var urlInfo in _dateUrls)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
                Thread.Sleep(_descriptor.RequestDelay);
                var html = _webClient.DownloadString(urlInfo.Url);
                _descriptor.Progress.BytesDownloaded += html.Length;
                _descriptor.Progress.PagesDownloaded += 1;
                var matches = Regex.Matches(html, pattern);
                foreach (Match m in matches)
                {
                    Thread.Sleep(_descriptor.RequestDelay);
                    var postInfo = new DateUrlInfo
                    {
                        Url = "http://" + m.Groups[1].Value,
                        PostDate = urlInfo.PostDate
                    };
                    DownloadPost(postInfo);

                }

                _descriptor.Progress.DatePagesProcessed += 1;
            }
            return true;
        }

        private void EnsureDirs()
        {
            var diaryDir = Path.Combine(_descriptor.WorkingDir, _diaryName);

            if (!Directory.Exists(diaryDir))
            {
                Directory.CreateDirectory(diaryDir);
            }

            var postDir = Path.Combine(diaryDir, "posts");
            if (!Directory.Exists(postDir))
            {
                Directory.CreateDirectory(postDir);
            }

            var imagesDir = Path.Combine(diaryDir, "images");
            if (!Directory.Exists(imagesDir))
            {
                Directory.CreateDirectory(imagesDir);
            }
        }

        private Dictionary<string, string> _imagesProcessed = new Dictionary<string, string>();

        private void DownloadPost(DateUrlInfo urlInfo)
        {
            var fileName = urlInfo.PostDate.ToString("yyyy-MM-dd") + "-" + Guid.NewGuid().ToString("n") + ".html";
            fileName = Path.Combine(_descriptor.WorkingDir, _diaryName, "posts", fileName);

            var html = _webClient.DownloadString(urlInfo.Url);
            _descriptor.Progress.BytesDownloaded += html.Length;
            _descriptor.Progress.PagesDownloaded += 1;
            using (var f = File.CreateText(fileName))
            {
                f.Write(html);
            }

            var matches = Regex.Matches(html, @"(https?:\/\/static.diary.ru[^\s""]*(gif|jpg|jpeg|png))");
            foreach (Match m in matches)
            {
                var imageUrl = m.Groups[1].Value;
                if (_imagesProcessed.ContainsKey(imageUrl))
                {
                    continue;
                }

                var fNameMatch = Regex.Match(imageUrl, @"([^\/]*)$");
                if (!fNameMatch.Success)
                {
                    Console.WriteLine($"Url skipped: {imageUrl}");
                    continue;
                }

                var imgFileName = Guid.NewGuid().ToString("n") + "-" + fNameMatch.Groups[1].Value;
                imgFileName = Path.Combine(_descriptor.WorkingDir, _diaryName, "images", imgFileName);

                var data = _webClient.DownloadData(imageUrl);
                _descriptor.Progress.BytesDownloaded += data.Length;
                _descriptor.Progress.ImagesDownloaded += 1;
                using (var f = File.Create(imgFileName))
                {
                    f.Write(data, 0, data.Length);
                }

                _imagesProcessed[imageUrl] = imgFileName;
            }
        }

        private void Login()
        {
            if (string.IsNullOrEmpty(_login) || string.IsNullOrEmpty(_pass))
            {
                return;
            }

            var html = _webClient.DownloadString("http://www.diary.ru/");
            var match = Regex.Match(html, @"<input[^>]*?name=""signature""[^>]*?value=""(\w+)\""[^>]*?>");
            if (!match.Success)
            {
                return;
            }

            var signature = match.Groups[1].Value;

            match = Regex.Match(html, @"action=""(\/login.php\?\w+)""");
            if (!match.Success)
            {
                return;
            }
            var url = match.Groups[1].Value;
            url = "http://www.diary.ru" + url;

            var coll = new NameValueCollection();
            coll["user_login"] = _login;
            coll["user_pass"] = _pass;
            coll["signature"] = signature;
            Thread.Sleep(_descriptor.RequestDelay);
            var data = _webClient.UploadValues(url, "POST", coll);
        }
    }

    public class DateUrlInfo
    {
        public DateTime PostDate { get; set; }
        public string Url { get; set; }
    }
}