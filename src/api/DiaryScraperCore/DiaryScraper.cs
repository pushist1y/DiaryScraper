using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cloudflare_Bypass;
using Microsoft.EntityFrameworkCore;

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
            CreateContext();

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

            _postsProcessed = _context.Posts.ToDictionary(p => p.Url, p => p);
            _imagesProcessed = _context.Images.ToDictionary(i => i.Url, i => i);

            if (!ScanDateUrls(cancellationToken))
            {
                _descriptor.Error = "Error checking calendar (probably not enough permissions)";
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _descriptor.Error = "Process was cancelled by user";
                return;
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
                        Console.WriteLine("Overwriting processed post: " + urlInfo.Url);
                        File.Delete(processedPost.LocalPath);
                        _context.Posts.Remove(processedPost);
                        _context.SaveChanges();
                        _postsProcessed.Remove(urlInfo.Url);
                    }
                    else
                    {
                        Console.WriteLine("Skipping processed post: " + urlInfo.Url);
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
            Console.WriteLine("Downloading post: " + urlInfo.Url);
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
            foreach (Match m in matches)
            {
                var imageUrl = m.Groups[1].Value;
                DownloadImage(imageUrl);
            }
        }

        private void DownloadImage(string imageUrl)
        {
            if (_imagesProcessed.TryGetValue(imageUrl, out var imageProcessed))
            {
                if (File.Exists(imageProcessed.LocalPath) )
                {
                    if (_descriptor.Overwrite && !imageProcessed.JustCreated)
                    {
                        Console.WriteLine("Overwriting processed image: " + imageUrl);
                        File.Delete(imageProcessed.LocalPath);
                        _context.Images.Remove(imageProcessed);
                        _context.SaveChanges();
                        _imagesProcessed.Remove(imageUrl);
                    }
                    else
                    {
                        Console.WriteLine("Skipping processed image: " + imageUrl);
                        return;
                    }
                }
            }

            var fNameMatch = Regex.Match(imageUrl, @"([^\/]*)$");
            if (!fNameMatch.Success)
            {
                Console.WriteLine($"Url skipped: {imageUrl}");
                return;
            }

            var imgFileName = Guid.NewGuid().ToString("n") + "-" + fNameMatch.Groups[1].Value;
            imgFileName = Path.Combine(_descriptor.WorkingDir, _diaryName, "images", imgFileName);

            Console.WriteLine("Downloading image: " + imageUrl);
            var data = _webClient.DownloadData(imageUrl);
            _descriptor.Progress.ImageDownloaded(data);
            using (var f = File.Create(imgFileName))
            {
                f.Write(data, 0, data.Length);
            }

            var image = new DiaryImage();
            image.Url = imageUrl;
            image.JustCreated = true;
            image.LocalPath = imgFileName;
            _context.Images.Add(image);
            _context.SaveChanges();
            _imagesProcessed[image.Url] = image;
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