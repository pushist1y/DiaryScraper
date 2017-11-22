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
            _downloader = new DataDownloader(Path.Combine(_options.WorkingDir, _options.DiaryName), _cookieContainer, _logger);
            _downloader.BeforeDownload += (s, e) =>
            {
                if (!(e.Resource is DiaryImage))
                {
                    Progress.CurrentUrl = e.Resource.Url;
                }
            };

            _downloader.AfterDownload += OnResourceDownloaded;
        }

        private void OnResourceDownloaded(object sender, DataDownloaderEventArgs args)
        {
            if (args.Resource is DiaryPost)
            {
                Progress.PageDownloaded(args.DownloadedData);
            }
            else if (args.Resource is DiaryImage)
            {
                Progress.ImageDownloaded(args.DownloadedData);
            }
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
            catch (AggregateException e)
            {
                if (e.InnerException is TaskCanceledException)
                {
                    Progress.Error = "Операция прервана пользователем";
                }
                else
                {
                    throw;
                }
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

            ScanDateUrls(dateUrls, cancellationToken).Wait();

        }

        private bool Login()
        {
            if (string.IsNullOrEmpty(_options.Login) || string.IsNullOrEmpty(_options.Password))
            {
                return false;
            }

            var html = _webClient.DownloadString("http://www.diary.ru/");
            var match = Regex.Match(html, @"<input[^>]*?name=""signature""[^>]*?value=""(\w+)\""[^>]*?>", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            var signature = match.Groups[1].Value;

            match = Regex.Match(html, @"action=""(\/login.php\?\w+)""", RegexOptions.IgnoreCase);
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
            if (Regex.IsMatch(html, @"неверное имя пользователя. Или неверный пароль", RegexOptions.IgnoreCase))
            {
                return false;
            }

            return true;
        }

        private IList<DiaryDatePage> GetDateUrls(CancellationToken cancellationToken)
        {
            var datePages = new List<DiaryDatePage>();
            var diaryUrl = $"http://{_options.DiaryName}.diary.ru";
            var calendarUrl = diaryUrl + "/?calendar";
            var html = _webClient.DownloadString(calendarUrl);
            Progress.PageDownloaded(html);
            var matches = Regex.Matches(html, @"calendar&year=(\d+)", RegexOptions.IgnoreCase);
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

                matches = Regex.Matches(html, @"diary.ru(\/\?date=(\d+)-(\d+)-(\d+))", RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    var date = new DateTime(Convert.ToInt32(m.Groups[2].Value),
                                            Convert.ToInt32(m.Groups[3].Value),
                                            Convert.ToInt32(m.Groups[4].Value));
                    if (date < _options.ScrapeStart || date > _options.ScrapeEnd)
                    {
                        continue;
                    }

                    datePages.Add(new DiaryDatePage { Url = diaryUrl + m.Groups[1].Value, PostDate = date });
                }
            }

            return datePages;
        }

        private async Task<bool> ScanDateUrls(IEnumerable<DiaryDatePage> dateUrls, CancellationToken cancellationToken)
        {
            var pattern = $@"({_options.DiaryName}\.diary\.ru\/p\w+\.htm).{{0,30}}URL";
            foreach (var urlInfo in dateUrls)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var downloadResult = await _downloader.Download(urlInfo, false, _options.RequestDelay);
                var html = downloadResult.DownloadedData.AsAnsiString();
                var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await DownloadPost("http://" + m.Groups[1].Value, urlInfo.PostDate);
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

            var downloadResult = await _downloader.Download(postInfo, false, _options.RequestDelay);

            var enc1251 = Encoding.GetEncoding(1251);
            var html = enc1251.GetString(downloadResult.DownloadedData);

            var matches = Regex.Matches(html, @"(https?:\/\/static.diary.ru[^\s""]*(gif|jpg|jpeg|png))", RegexOptions.IgnoreCase);
            var imageUrls = matches.Select(m2 => m2.Groups[1].Value).ToList();
            var diaryImages = await _imageChecker.FilterUrls(imageUrls.Distinct(), _options.Overwrite);
            foreach (var img in diaryImages)
            {
                img.GenerateLocalPath(Guid.NewGuid().ToString("n") + "-");
            }
            var downloadResults = await _downloader.Download(diaryImages, true, 0);
            await _imageChecker.AddProcessedData(downloadResults.Select(d => d.Resource as DiaryImage));
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

}