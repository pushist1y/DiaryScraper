using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cloudflare_Bypass;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public class ScraperFinishedArguments
    { }
    public class DiaryScraperNew
    {
        public event EventHandler<ScraperFinishedArguments> ScrapeFinished;
        public Task Worker;
        public CancellationTokenSource TokenSource;
        private readonly ILogger<DiaryScraperNew> _logger;
        private readonly CookieContainer _cookieContainer;
        private readonly CF_WebClient _webClient;
        public readonly ScrapeTaskProgress Progress = new ScrapeTaskProgress();
        private readonly DiaryScraperOptions _options;
        private readonly ScrapeContext _context;
        private readonly DownloadExistingChecker<DiaryPost> _postChecker;
        private readonly DownloadExistingChecker<DiaryImage> _imageChecker;
        private readonly DataDownloader _downloader;
        public DiaryScraperNew(ILogger<DiaryScraperNew> logger, ScrapeContext context, DiaryScraperOptions options)
        {
            _logger = logger;
            _cookieContainer = new CookieContainer();
            _webClient = new CF_WebClient(_cookieContainer);
            TokenSource = new CancellationTokenSource();

            _context = context;
            _options = options;
            _postChecker = new DownloadExistingChecker<DiaryPost>(Path.Combine(_options.WorkingDir, _options.DiaryName), _context, logger);
            _imageChecker = new DownloadExistingChecker<DiaryImage>(Path.Combine(_options.WorkingDir, _options.DiaryName), _context, logger);
            _downloader = new DataDownloader(Path.Combine(_options.WorkingDir, _options.DiaryName), _cookieContainer, _logger, Progress);
        }

        public Task Run()
        {
            Worker = new Task(() => DoWorkWrapped(TokenSource.Token));
            Worker.Start();
            return Worker;
        }

        public void DoWorkWrapped(CancellationToken cancellationToken)
        {
            try
            {
                DoWork(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Progress.Error = "Операция прервана пользователем";
            }
            catch (Exception e)
            {
                Progress.Error = e.Message;
                _logger.LogError(e, "Error");
                throw;
            }
            finally
            {
                ScrapeFinished?.Invoke(this, new ScraperFinishedArguments());
            }
        }

        public void DoWork(CancellationToken cancellationToken)
        {
            if (!Login())
            {
                throw new Exception("Авторизация не удалась");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var dateUrls = GetDateUrls(cancellationToken);
            Progress.DatePagesDiscovered = dateUrls.Count;

            _postChecker.InitializeFromContext();
            _imageChecker.InitializeFromContext();

            ScanDateUrls(dateUrls, cancellationToken);

        }

        private bool Login()
        {
            if (string.IsNullOrEmpty(_options.Login) || string.IsNullOrEmpty(_options.Password))
            {
                return false;
            }

            var html = _webClient.DownloadString("http://www.diary.ru/");
            var match = Regex.Match(html, @"<input[^>]*?name=""signature""[^>]*?value=""(\w+)\""[^>]*?>");
            if (!match.Success)
            {
                return false;
            }

            var signature = match.Groups[1].Value;

            match = Regex.Match(html, @"action=""(\/login.php\?\w+)""");
            if (!match.Success)
            {
                return false;
            }
            var url = match.Groups[1].Value;
            url = "http://www.diary.ru" + url;

            var reqdata = HttpWebExtensions.GetDiaryLoginPostData(_options.Login, _options.Password, signature);

            Thread.Sleep(_options.RequestDelay);
            _webClient.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            var data = _webClient.UploadData(url, "POST", reqdata);
            html = data.AsAnsiString();
            if (Regex.IsMatch(html, @"неверное имя пользователя. Или неверный пароль"))
            {
                return false;
            }

            return true;
        }

        private IList<DateUrlInfo> GetDateUrls(CancellationToken cancellationToken)
        {
            var dateUrls = new List<DateUrlInfo>();
            var diaryUrl = $"http://{_options.DiaryName}.diary.ru";
            var calendarUrl = diaryUrl + "/?calendar";
            var html = _webClient.DownloadString(calendarUrl);
            Progress.PageDownloaded(html);
            var matches = Regex.Matches(html, @"calendar&year=(\d+)");
            if (matches.Count <= 0)
            {
                return null;
            }
            var yearUrls = new List<string>();
            foreach (Match m in matches)
            {
                var strYear = m.Groups[1].Value;
                var yearStart = new DateTime(Convert.ToInt32(strYear), 1, 1);
                var yearEnd = new DateTime(Convert.ToInt32(strYear), 12, 31);
                if (yearStart <= _options.ScrapeEnd && yearEnd >= _options.ScrapeStart)
                {
                    yearUrls.Add(diaryUrl + $"/?calendar&year={strYear}");
                }
            }

            foreach (var yearUrl in yearUrls)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(_options.RequestDelay); // delay web requests
                html = _webClient.DownloadString(yearUrl);

                Progress.PageDownloaded(html);

                matches = Regex.Matches(html, @"diary.ru(\/\?date=(\d+)-(\d+)-(\d+))");
                foreach (Match m in matches)
                {
                    var date = new DateTime(Convert.ToInt32(m.Groups[2].Value),
                                            Convert.ToInt32(m.Groups[3].Value),
                                            Convert.ToInt32(m.Groups[4].Value));
                    if (date < _options.ScrapeStart || date > _options.ScrapeEnd)
                    {
                        continue;
                    }

                    dateUrls.Add(new DateUrlInfo { Url = diaryUrl + m.Groups[1].Value, PostDate = date });
                }
            }

            return dateUrls;
        }

        private bool ScanDateUrls(IEnumerable<DateUrlInfo> dateUrls, CancellationToken cancellationToken)
        {
            var pattern = $@"({_options.DiaryName}\.diary\.ru\/p\w+\.htm).{{0,30}}URL";
            foreach (var urlInfo in dateUrls)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(_options.RequestDelay);
                cancellationToken.ThrowIfCancellationRequested();
                var html = _webClient.DownloadString(urlInfo.Url);

                Progress.PageDownloaded(html);

                var matches = Regex.Matches(html, pattern);
                foreach (Match m in matches)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DownloadPost("http://" + m.Groups[1].Value, urlInfo.PostDate).Wait();
                }

                Progress.DatePagesProcessed += 1;
            }
            return true;
        }

        private async Task DownloadPost(string url, DateTime date)
        {
            var postInfo = await _postChecker.CheckUrl(url, _options.Overwrite);

            if (postInfo == null)
            {
                return;
            }

            postInfo.GenerateLocalPath(date.ToString("yyyy-MM-dd") + "-");

            await _downloader.Download(postInfo, true, false, _options.RequestDelay);

            var enc1251 = Encoding.GetEncoding(1251);
            var html = enc1251.GetString(postInfo.Data);

            var matches = Regex.Matches(html, @"(https?:\/\/static.diary.ru[^\s""]*(gif|jpg|jpeg|png))");
            var imageUrls = matches.Select(m2 => m2.Groups[1].Value).ToList();
            var diaryImages = await _imageChecker.FilterUrls(imageUrls.Distinct(), _options.Overwrite);
            foreach (var img in diaryImages)
            {
                img.GenerateLocalPath(Guid.NewGuid().ToString("n") + "-");
            }
            await _downloader.Download(diaryImages, false, true, 0);
            await _imageChecker.AddProcessedData(diaryImages);
            await _postChecker.AddProcessedData(postInfo);

        }
    }

    public class DiaryScraperOptions
    {
        public string WorkingDir { get; set; }
        public string DiaryName { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public int RequestDelay { get; set; }
        public DateTime ScrapeStart { get; set; }
        public DateTime ScrapeEnd { get; set; }
        public bool Overwrite { get; set; }

    }

    public class DateUrlInfo
    {
        public DateTime PostDate { get; set; }
        public string Url { get; set; }
    }
}