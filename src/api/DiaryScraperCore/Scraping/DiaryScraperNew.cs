using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Parser.Html;
using AngleSharp.Xml;
using Cloudflare_Bypass;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public class DiaryScraperNew : DiaryAsyncImplementationBase
    {
        private readonly ILogger<DiaryScraperNew> _logger;
        private readonly CookieContainer _cookieContainer;
        private readonly CF_WebClient _webClient;
        public readonly ScrapeTaskProgress Progress = new ScrapeTaskProgress();
        private readonly DiaryScraperOptions _options;
        private readonly ScrapeContext _context;
        private readonly DownloadExistingChecker _downloadExistingChecker;
        private readonly DataDownloader _downloader;
        private readonly DiaryMoreLinksFixer _moreFixer;
        private readonly HtmlParser _parser;

        protected override ILogger Logger => _logger;

        public DiaryScraperNew(ILogger<DiaryScraperNew> logger, ScrapeContext context, DiaryScraperOptions options)
        {
            _logger = logger;
            _cookieContainer = new CookieContainer();
            _webClient = new CF_WebClient(_cookieContainer);


            _context = context;
            _options = options;
            _downloadExistingChecker = new DownloadExistingChecker(Path.Combine(_options.WorkingDir, _options.DiaryName), context, _logger);
            _downloader = new DataDownloader($"http://{_options.DiaryName}.diary.ru",
                                        Path.Combine(_options.WorkingDir, _options.DiaryName),
                                        _cookieContainer,
                                        _logger);

            _downloader.BeforeDownload += (s, e) =>
            {
                if (!(e.Resource is DiaryImage))
                {
                    Progress.Values[ScrapeProgressNames.CurrentUrl] = e.Resource.Url.ToLower();
                }
            };

            _downloader.AfterDownload += OnResourceDownloaded;
            var config = new Configuration().WithCss();
            _parser = new HtmlParser(config);
            _moreFixer = new DiaryMoreLinksFixer(_downloader, _options.WorkingDir, _options.DiaryName);
        }

        public override void SetError(string error)
        {
            Progress.Error = error;
        }

        private void OnResourceDownloaded(object sender, DataDownloaderEventArgs args)
        {
            if (args.Resource is DiaryPost || args.Resource is DiaryDatePage || args.Resource is DiaryAccountPage)
            {
                Progress.PageDownloaded(args.DownloadedData);
            }
            else if (args.Resource is DiaryImage)
            {
                Progress.ImageDownloaded(args.DownloadedData);
            }
        }


        public override void DoWork(CancellationToken cancellationToken)
        {
            if (!Login())
            {
                throw new Exception("Авторизация не удалась");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var dateUrls = GetDateUrls(cancellationToken);
            Progress.Values[ScrapeProgressNames.DatePagesDiscovered] = dateUrls.Count;

            if (_options.DownloadAccount)
            {
                DownloadMetadataPages(cancellationToken).Wait();
            }
            DownloadLastPostsPage(cancellationToken).Wait();

            Progress.RangeDiscovered = true;

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

                Progress.IncrementInt(ScrapeProgressNames.DatePagesProcessed, 1);
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
            var html = downloadResult.DownloadedData.AsAnsiString();
            var doc = _parser.Parse(html);

            if (_options.DownloadEdits)
            {
                var editLink = doc.QuerySelector("li.editPostLink a");
                if (editLink != null)
                {
                    var editUrl = editLink.GetAttribute("href");
                    var editInfo = new DiaryPostEdit { Url = editUrl };
                    editInfo.Post = postInfo;
                    postInfo.PostEdit = editInfo;
                    editInfo.GenerateLocalPath(date.ToString("yyyy-MM-dd") + "-");
                    var editDownloadResult = await _downloader.Download(editInfo, false, _options.RequestDelay);
                }
            }

            if (await _moreFixer.FixPage(doc) && !string.IsNullOrEmpty(downloadResult.Resource.RelativePath))
            {
                var filePath = Path.Combine(_options.WorkingDir, _options.DiaryName, downloadResult.Resource.RelativePath);
                doc.WriteToFile(filePath, Encoding.GetEncoding(1251));
            }

            html = doc.GetHtml();

            var imageUrls = doc.QuerySelectorAll("img").Select(i => i.GetAttribute("src")).Distinct().ToList();
            imageUrls = FilterImageUrls(imageUrls).ToList();
            // var matches = Regex.Matches(html, @"(https?:\/\/static.diary.ru[^\s""]*(gif|jpg|jpeg|png))", RegexOptions.IgnoreCase);
            // var imageUrls = matches.Select(m2 => m2.Groups[1].Value).ToList();
            var diaryImages = await _downloadExistingChecker.FilterUrlsAsync<DiaryImage>(imageUrls.Distinct(), _options.Overwrite);
            foreach (var img in diaryImages)
            {
                img.GenerateLocalPath(Guid.NewGuid().ToString("n") + "-");
            }
            var downloadResults = await _downloader.Download(diaryImages, true, 0);
            await _downloadExistingChecker.AddProcessedDataAsync(downloadResults.Select(d => d.Resource as DiaryImage));
            await _downloadExistingChecker.AddProcessedDataAsync(postInfo);

        }


        private async Task DownloadLastPostsPage(CancellationToken cancellationToken)
        {
            var pages = new Dictionary<string, string>{
                {"http://www.diary.ru/?last_post", AccountPagesFileNames.LastPosts},
                {$"http://{_options.DiaryName}.diary.ru/?favorite", AccountPagesFileNames.Favorite},
                {"/favicon.ico", AccountPagesFileNames.Favicon}
            };

            var sPages = await _downloadExistingChecker.FilterUrlsAsync<DiaryAccountPage>(pages.Keys, _options.Overwrite);
            var cssUrls = new List<string>();
            var imageUrls = new List<string>();

            foreach (var sPage in sPages)
            {
                sPage.GenerateLocalPath(pages[sPage.Url]);
                var dRes = await _downloader.Download(sPage, false, _options.RequestDelay);

                using (var doc = await _parser.ParseAsync(dRes.DownloadedData.AsAnsiString()))
                {
                    cssUrls.AddRange(doc.QuerySelectorAll("link[rel='stylesheet']").Select(l => l.GetAttribute("href")));
                    imageUrls.AddRange(doc.QuerySelectorAll("img").Select(i => i.GetAttribute("src")).Where(s => s.Contains("diary.ru") || s.StartsWith("/")));
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            var cssPages = await _downloadExistingChecker.FilterUrlsAsync<DiaryAccountPage>(cssUrls, _options.Overwrite);
            foreach (var cssPage in cssPages)
            {
                var guid = Guid.NewGuid().ToString("n");
                cssPage.GenerateLocalPath($"style_{guid}.css");
            }
            await _downloader.Download(cssPages, false);

            imageUrls = FilterImageUrls(imageUrls).ToList();
            var images = await _downloadExistingChecker.FilterUrlsAsync<DiaryImage>(imageUrls, _options.Overwrite);
            foreach (var img in images)
            {
                img.GenerateLocalPath(Guid.NewGuid().ToString("n") + "-");
            }
            await _downloader.Download(images, true, 0);

            await _downloadExistingChecker.AddProcessedDataAsync(sPages as IEnumerable<DiaryAccountPage>);
            await _downloadExistingChecker.AddProcessedDataAsync(cssPages as IEnumerable<DiaryAccountPage>);
            await _downloadExistingChecker.AddProcessedDataAsync(images as IEnumerable<DiaryImage>);
        }

        private IEnumerable<string> FilterImageUrls(IEnumerable<string> imageUrls)
        {
            return imageUrls.Where(s =>
                        !string.IsNullOrEmpty(s) &&
                        !s.Contains("favicon.ico") &&
                        (s.Contains("diary.ru") || (s.StartsWith("/") && !s.StartsWith("//")))
            ).Distinct();
        }

        private async Task DownloadMetadataPages(CancellationToken cancellationToken)
        {
            var dr = new DiaryAccountPage() { Url = "http://www.diary.ru" };
            dr.GenerateLocalPath(AccountPagesFileNames.DiaryMain);
            var dRes = await _downloader.Download(dr, false, _options.RequestDelay);
            var doc = await _parser.ParseAsync(dRes.DownloadedData.AsAnsiString());
            var href = doc.QuerySelector("a[title='профиль']").GetAttribute("href");
            var userId = Regex.Replace(href, @"\/member\/\?(\d+)$", "$1");
            cancellationToken.ThrowIfCancellationRequested();

            var urls = new Dictionary<string, string>{
                {"http://www.diary.ru/options/member/?access", AccountPagesFileNames.MemberAccess},
                {"http://www.diary.ru/options/diary/?access", AccountPagesFileNames.DiaryAccess},
                {"http://www.diary.ru/options/diary/?commentaccess", AccountPagesFileNames.DiaryCommentAccess},
                {"http://www.diary.ru/options/diary/?pch", AccountPagesFileNames.DiaryPch},
                {$"http://www.diary.ru/member/?{userId}&fullreaderslist&fullfavoriteslist&fullcommunity_membershiplist&fullcommunity_moderatorslist&fullcommunity_masterslist&fullcommunity_memberslist", AccountPagesFileNames.Member},
                {"http://www.diary.ru/options/diary/?tags", AccountPagesFileNames.Tags},
                {"http://www.diary.ru/options/member/?profile", AccountPagesFileNames.Profile},
                {"http://www.diary.ru/options/member/?geography", AccountPagesFileNames.Geography},
                {"http://www.diary.ru/options/diary/?owner", AccountPagesFileNames.Owner}
            };
            var sPages = await _downloadExistingChecker.FilterUrlsAsync<DiaryAccountPage>(urls.Keys, _options.Overwrite);
            foreach (var sPage in sPages)
            {
                sPage.GenerateLocalPath(urls[sPage.Url]);
                dRes = await _downloader.Download(sPage);
                cancellationToken.ThrowIfCancellationRequested();
            }
            await _downloadExistingChecker.AddProcessedDataAsync(sPages as IEnumerable<DiaryAccountPage>);

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
        public bool DownloadEdits { get; set; }
        public bool DownloadAccount { get; set; }

    }

}
