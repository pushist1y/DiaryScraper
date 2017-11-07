using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
            var match = Regex.Match(_descriptor.DiaryUrl, @"(\w+)\.diary\.ru");
            if (!match.Success)
            {
                return false;
            }
            _diaryBaseUrl = $"http://{match.Groups[1].Value}.diary.ru";
            return true;
        }

        private List<string> _dateUrls = new List<string>();
        private bool FillDateUrls()
        {
            var calendarUrl = _diaryBaseUrl + "/?calendar";
            var html = _webClient.DownloadString(calendarUrl);
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
                Thread.Sleep(2000); // delay web requests
                html = _webClient.DownloadString(yearUrl);
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

                    _dateUrls.Add(_diaryBaseUrl + m.Groups[1].Value);
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

            if (!string.IsNullOrEmpty(_login) && !string.IsNullOrEmpty(_pass))
            {
                Login();
            }

            if (!FillDateUrls())
            {
                _descriptor.Error = "Error checking calendar (probably not enough permissions)";
                return;
            }

            _descriptor.Progress.DatePagesDiscovered = _dateUrls.Count;

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

                Thread.Sleep(1000);
                _descriptor.Progress.LastUpdated = DateTime.Now;
                _descriptor.Progress.BytesDownloaded += 1024;
            }
        }

        private void ScanDateUrls()
        {
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
            Thread.Sleep(2000);
            var data = _webClient.UploadValues(url, "POST", coll);
        }
    }
}