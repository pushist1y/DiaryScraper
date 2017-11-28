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
        private readonly DownloadExistingChecker _downloadExistingChecker;
        private readonly DataDownloader _downloader;
        private readonly DiaryMoreLinksFixer _moreFixer;
        public DiaryScraperNew(ILogger<DiaryScraperNew> logger, ScrapeContext context, DiaryScraperOptions options)
        {
            _logger = logger;
            _cookieContainer = new CookieContainer();
            _webClient = new CF_WebClient(_cookieContainer);
            TokenSource = new CancellationTokenSource();

            _context = context;
            _options = options;
            _downloadExistingChecker = new DownloadExistingChecker(Path.Combine(_options.WorkingDir, _options.DiaryName), context, _logger);
            _downloader = new DataDownloader(Path.Combine(_options.WorkingDir, _options.DiaryName), _cookieContainer, _logger);
            _downloader.BeforeDownload += (s, e) =>
            {
                if (!(e.Resource is DiaryImage))
                {
                    Progress.CurrentUrl = e.Resource.Url.ToLower();
                }
            };

            _downloader.AfterDownload += OnResourceDownloaded;
            
            _moreFixer = new DiaryMoreLinksFixer(_downloader, _options.WorkingDir, _options.DiaryName);
        }

        private void OnResourceDownloaded(object sender, DataDownloaderEventArgs args)
        {
            if (args.Resource is DiaryPost || args.Resource is DiaryDatePage)
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
                    Progress.Error = e.InnerException.Message;
                    _logger.LogError(e.InnerException, "Error");
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

            ScanDateUrlsAsync(dateUrls, cancellationToken).Wait();

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

                    var datePage = _downloadExistingChecker.CheckUrlAsync<DiaryDatePage>(diaryUrl + m.Groups[1].Value, _options.Overwrite).Result;
                    if (datePage != null)
                    {
                        datePage.PostDate = date;
                        datePages.Add(datePage);
                    }
                }
            }

            return datePages;
        }

        private async Task<bool> ScanDateUrlsAsync(IEnumerable<DiaryDatePage> datePages, CancellationToken cancellationToken)
        {
            var pattern = $@"({_options.DiaryName}\.diary\.ru\/p\w+\.htm).{{0,30}}URL";
            foreach (var datePage in datePages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var downloadResult = await _downloader.Download(datePage, false, _options.RequestDelay);
                var html = downloadResult.DownloadedData.AsAnsiString();
                var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await DownloadPostAsync("http://" + m.Groups[1].Value, datePage.PostDate);
                }

                Progress.DatePagesProcessed += 1;
                await _downloadExistingChecker.AddProcessedDataAsync(datePage);
            }
            return true;
        }

        private async Task DownloadPostAsync(string url, DateTime date)
        {
            var postInfo = await _downloadExistingChecker.CheckUrlAsync<DiaryPost>(url, _options.Overwrite);

            if (postInfo == null)
            {
                return;
            }

            postInfo.GenerateLocalPath(date.ToString("yyyy-MM-dd") + "-");

            var downloadResult = await _downloader.Download(postInfo, false, _options.RequestDelay);

            var enc1251 = Encoding.GetEncoding(1251);
            var html = enc1251.GetString(downloadResult.DownloadedData);
            await _moreFixer.FixPage(downloadResult);

            var matches = Regex.Matches(html, @"(https?:\/\/static.diary.ru[^\s""]*(gif|jpg|jpeg|png))", RegexOptions.IgnoreCase);
            var imageUrls = matches.Select(m2 => m2.Groups[1].Value).ToList();
            var diaryImages = await _downloadExistingChecker.FilterUrlsAsync<DiaryImage>(imageUrls.Distinct(), _options.Overwrite);
            foreach (var img in diaryImages)
            {
                img.GenerateLocalPath(Guid.NewGuid().ToString("n") + "-");
            }
            var downloadResults = await _downloader.Download(diaryImages, true, 0);
            await _downloadExistingChecker.AddProcessedDataAsync(downloadResults.Select(d => d.Resource as DiaryImage));
            await _downloadExistingChecker.AddProcessedDataAsync(postInfo);

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