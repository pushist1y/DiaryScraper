using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cloudflare_Bypass;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Targets;

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
        private ScrapeContext _context;
        private readonly CookieContainer _cookieContainer;
        public DiaryScraper(ScrapeTaskDescriptor descriptor, ILogger<DiaryScraper> logger) : this(descriptor, null, null, logger)
        {

        }

        private readonly ILogger<DiaryScraper> _logger;
        public DiaryScraper(ScrapeTaskDescriptor descriptor, string login, string pass, ILogger<DiaryScraper> logger)
        {
            _logger = logger;
            _login = login;
            _pass = pass;
            _descriptor = descriptor;
            _cookieContainer = new CookieContainer();
            _webClient = new CF_WebClient(_cookieContainer);
        }

        public Task Run()
        {
            var src = new CancellationTokenSource();
            _descriptor.TokenSource = src;
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
            _descriptor.Progress.PageDownloaded(html);
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

                _descriptor.Progress.PageDownloaded(html);

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

        private void CreateContext()
        {
            var dbPath = Path.Combine(_descriptor.WorkingDir, _diaryName, "scrape.db");
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite($@"Data Source={dbPath}");
            _context = new ScrapeContext(optionsBuilder.Options);
            _context.Database.Migrate();
        }


        private void DoWork(CancellationToken cancellationToken)
        {
            try
            {
                var startTime = DateTime.Now;

                if (_descriptor.ScrapeStart > _descriptor.ScrapeEnd)
                {
                    SetError("Scrape interval provided is wrong", cancellationToken);
                    return;
                }

                if (!CheckDiaryUrl())
                {
                    SetError("Diary url is wrong", cancellationToken);
                    return;
                }

                EnsureDirs();
                ConfigureLog();
                CreateContext();

                if (!string.IsNullOrEmpty(_login) && !string.IsNullOrEmpty(_pass))
                {
                    if (!Login())
                    {
                        SetError("Login failed", cancellationToken);
                        return;
                    }
                }

                if (!FillDateUrls(cancellationToken))
                {
                    SetError("Error checking calendar (probably not enough permissions)", cancellationToken);
                    return;
                }

                _descriptor.Progress.DatePagesDiscovered = _dateUrls.Count;

                _postsProcessed = _context.Posts.ToDictionary(p => p.Url, p => p);
                _imagesProcessed = _context.Images.ToDictionary(i => i.Url, i => i);

                if (!ScanDateUrls(cancellationToken))
                {
                    SetError("Ошибка при скачивании постов", cancellationToken);
                    return;
                }
            }
            catch (Exception e)
            {
                SetError(e.Message, cancellationToken);
                _logger.LogError(e, "Error");
                throw;
            }
            finally
            {
                UnsetLog();
            }

        }

        private void SetError(string errorText, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _descriptor.Error = "Операция прервана по запросу пользователя";
            }
            else
            {
                _descriptor.Error = errorText;
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

                _descriptor.Progress.PageDownloaded(html);

                var matches = Regex.Matches(html, pattern);
                foreach (Match m in matches)
                {

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

        private FileTarget _errorLogTarget;
        private LoggingRule _errorLogRule;

        private void ConfigureLog()
        {
            _errorLogTarget = new FileTarget();
            _errorLogTarget.Name = "errorTarget_" + Guid.NewGuid().ToString("n");
            _errorLogTarget.FileName = Path.Combine(_descriptor.WorkingDir, "${shortdate}.log");
            _errorLogTarget.Layout = @"${date:format=dd.MM.yyyy HH\:mm\:ss} (${level:uppercase=true}): ${message}. ${exception:format=ToString}";
            NLog.LogManager.Configuration.AddTarget(_errorLogTarget);

            _errorLogRule = new LoggingRule("DiaryScraperCore*", NLog.LogLevel.Warn, _errorLogTarget);
            NLog.LogManager.Configuration.LoggingRules.Add(_errorLogRule);

            NLog.LogManager.ReconfigExistingLoggers();
        }

        private void UnsetLog()
        {
            if (_errorLogTarget != null)
            {
                NLog.LogManager.Configuration.RemoveTarget(_errorLogTarget.Name);
            }
            if (_errorLogRule != null)
            {
                NLog.LogManager.Configuration.LoggingRules.Remove(_errorLogRule);
            }
            NLog.LogManager.ReconfigExistingLoggers();
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

        private Dictionary<string, DiaryPost> _postsProcessed = new Dictionary<string, DiaryPost>();
        private Dictionary<string, DiaryImage> _imagesProcessed = new Dictionary<string, DiaryImage>();

        private void DownloadPost(DateUrlInfo urlInfo)
        {
            if (_postsProcessed.TryGetValue(urlInfo.Url, out var processedPost))
            {
                if (File.Exists(processedPost.LocalPath))
                {
                    if (_descriptor.Overwrite)
                    {
                        _logger.LogInformation("Overwriting processed post: " + urlInfo.Url);
                        File.Delete(processedPost.LocalPath);
                        _context.Posts.Remove(processedPost);
                        _context.SaveChanges();
                        _postsProcessed.Remove(urlInfo.Url);
                    }
                    else
                    {
                        _logger.LogInformation("Skipping processed post: " + urlInfo.Url);
                        return;
                    }
                }
            }

            var fNameMatch = Regex.Match(urlInfo.Url, @"([^\/]*)$");
            if (!fNameMatch.Success)
            {
                return;
            }
            var fileName = urlInfo.PostDate.ToString("yyyy-MM-dd") + "-" + fNameMatch.Groups[1].Value;
            fileName = Path.Combine(_descriptor.WorkingDir, _diaryName, "posts", fileName);

            Thread.Sleep(_descriptor.RequestDelay);
            _logger.LogInformation("Downloading post: " + urlInfo.Url);
            var bytes = _webClient.DownloadData(urlInfo.Url);

            _descriptor.Progress.PageDownloaded(bytes);
            using (var f = File.Create(fileName))
            {
                f.Write(bytes, 0, bytes.Length);
            }

            var post = new DiaryPost();
            post.Url = urlInfo.Url;
            post.LocalPath = fileName;
            _context.Posts.Add(post);
            _context.SaveChanges();
            _postsProcessed[post.Url] = post;


            var enc1251 = Encoding.GetEncoding(1251);
            var html = enc1251.GetString(bytes);

            var matches = Regex.Matches(html, @"(https?:\/\/static.diary.ru[^\s""]*(gif|jpg|jpeg|png))");
            var imageUrls = matches.Select(m => m.Groups[1].Value).ToList();
            DownloadImagesAsync(imageUrls).Wait();
        }

        private async Task<List<string>> CheckImageUrls(IEnumerable<string> imageUrls)
        {
            var downloadingImages = new List<string>();
            foreach (var imageUrl in imageUrls)
            {
                if (_imagesProcessed.TryGetValue(imageUrl, out var imageProcessed))
                {
                    if (File.Exists(imageProcessed.LocalPath))
                    {
                        if (_descriptor.Overwrite && !imageProcessed.JustCreated)
                        {
                            _logger.LogInformation("Overwriting processed image: " + imageUrl);
                            File.Delete(imageProcessed.LocalPath);
                            _context.Images.Remove(imageProcessed);
                            _imagesProcessed.Remove(imageUrl);
                            downloadingImages.Add(imageUrl);
                        }
                        else
                        {
                            _logger.LogInformation("Skipping processed image: " + imageUrl);
                            continue;
                        }
                    }
                }
                else
                {
                    downloadingImages.Add(imageUrl);
                }
            }

            await _context.SaveChangesAsync();

            return downloadingImages;
        }

        private async Task DownloadImagesAsync(IEnumerable<string> imageUrls)
        {
            var imagesToDownload = await CheckImageUrls(imageUrls.Distinct());
            var tasks = imagesToDownload.Select(url => DownloadImageAsync(url));
            var imgs = new List<DiaryImage>();
            foreach (var task in tasks)
            {
                var img = await task;
                if (img != null)
                {
                    imgs.Add(img);
                }
            }
            await _context.Images.AddRangeAsync(imgs);
            await _context.SaveChangesAsync();
        }

        private async Task<DiaryImage> DownloadImageAsync(string imageUrl)
        {
            var fNameMatch = Regex.Match(imageUrl, @"([^\/]*)$");
            if (!fNameMatch.Success)
            {
                _logger.LogInformation($"Url skipped: {imageUrl}");
                return null;
            }

            var imgFileName = Guid.NewGuid().ToString("n") + "-" + fNameMatch.Groups[1].Value;
            imgFileName = Path.Combine(_descriptor.WorkingDir, _diaryName, "images", imgFileName);

            _logger.LogInformation("Downloading image: " + imageUrl);
            var uri = new Uri(imageUrl);
            var client = new CF_WebClient(_cookieContainer);
            try
            {
                var data = await client.DownloadDataTaskAsync(uri);

                _descriptor.Progress.ImageDownloaded(data);
                using (var f = File.Create(imgFileName))
                {
                    await f.WriteAsync(data, 0, data.Length);
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = e.Response as HttpWebResponse;
                    if (response != null)
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            _logger.LogWarning("Url not found: " + e.Response.ResponseUri.AbsoluteUri);
                            imgFileName = "";
                        }
                    }
                }
            }

            var image = new DiaryImage();
            image.Url = imageUrl;
            image.JustCreated = true;
            image.LocalPath = imgFileName;
            //_context.Images.Add(image);
            //await _context.SaveChangesAsync();
            _imagesProcessed[image.Url] = image;
            return image;
        }

        private bool Login()
        {
            if (string.IsNullOrEmpty(_login) || string.IsNullOrEmpty(_pass))
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

            var reqdata = HttpWebExtensions.GetDiaryLoginPostData(_login, _pass, signature);

            Thread.Sleep(_descriptor.RequestDelay);
            _webClient.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            var data = _webClient.UploadData(url, "POST", reqdata);
            html = data.AsAnsiString();
            if (Regex.IsMatch(html, @"неверное имя пользователя. Или неверный пароль"))
            {
                return false;
            }

            return true;
        }
    }

    public class DateUrlInfo
    {
        public DateTime PostDate { get; set; }
        public string Url { get; set; }
    }
}