using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Parser.Html;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public class DiaryArchiver : DiaryAsyncImplementationBase
    {
        private readonly ILogger<DiaryArchiver> _logger;
        public ArchiveTaskProgress Progress = new ArchiveTaskProgress();
        private readonly ArchiveTaskDescriptor _descriptor;
        private readonly ScrapeContext _context;
        private readonly HtmlParser _parser;
        private string DiaryDir => _descriptor.WorkingDir;
        private string ArchiveDir => Path.Combine(DiaryDir, Constants.ArchiveDir);
        private string PostsDir => Path.Combine(DiaryDir, Constants.PostsDir);
        private string ImagesDir => Path.Combine(DiaryDir, Constants.ImagesDir);
        private string AccountPagesDir => Path.Combine(DiaryDir, Constants.AccountPagesDir);
        private string ArchivePostsDir => Path.Combine(ArchiveDir, Constants.PostsDir);
        private string ArchiveImagesDir => Path.Combine(ArchiveDir, Constants.ImagesDir);
        private string ArchiveAccountDir => Path.Combine(ArchiveDir, Constants.AccountPagesDir);
        public DiaryArchiver(ILogger<DiaryArchiver> logger, ArchiveTaskDescriptor descriptor, ScrapeContext context)
        {
            _logger = logger;
            _descriptor = descriptor;
            var config = new Configuration().WithCss();
            _parser = new HtmlParser(config);
            _context = context;
        }

        protected override ILogger Logger => _logger;

        public override void DoWork(CancellationToken cancellationToken)
        {
            DoWorkAsync(cancellationToken).Wait();
        }

        public async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(ArchivePostsDir);
            Directory.CreateDirectory(ArchiveImagesDir);
            Directory.CreateDirectory(ArchiveAccountDir);


            Progress.Values[ParseProgressNames.CurrentFile] = "Копирование файлов";
            CopyDir(PostsDir, ArchivePostsDir);
            cancellationToken.ThrowIfCancellationRequested();
            CopyDir(ImagesDir, ArchiveImagesDir);
            cancellationToken.ThrowIfCancellationRequested();
            CopyDir(AccountPagesDir, ArchiveAccountDir);

            var assembly = Assembly.GetEntryAssembly();

            using (var fileStream = File.Create(Path.Combine(ArchiveAccountDir, "jquery-3.2.1.min.js")))
            using (var resourceStream = assembly.GetManifestResourceStream("DiaryScraperCore.Resources.jquery-3.2.1.min.js"))
            {
                resourceStream.Position = 0;
                resourceStream.CopyTo(fileStream);
                fileStream.Close();
            }

            using (var fileStream = File.Create(Path.Combine(ArchiveAccountDir, "diary-scraper.js")))
            using (var resourceStream = assembly.GetManifestResourceStream("DiaryScraperCore.Resources.diary-scraper.js"))
            {
                resourceStream.Position = 0;
                resourceStream.CopyTo(fileStream);
                fileStream.Close();
            }

            Progress.Values[ParseProgressNames.CurrentFile] = "Подготовка шаблона";

            var templatePath = Path.Combine(_descriptor.WorkingDir, Constants.AccountPagesDir, AccountPagesFileNames.LastPosts);
            var indexPath = Path.Combine(ArchiveDir, "index.htm");
            File.Copy(templatePath, indexPath);

            var indexDoc = await _parser.FromFileAsync(indexPath, Encoding.GetEncoding(1251), cancellationToken);
            foreach (var postDiv in indexDoc.QuerySelectorAll("div.singlePost"))
            {
                postDiv.Remove();
            }

            var barDiv = indexDoc.QuerySelector("div#pageBar");

            var postFiles = Directory.GetFiles(ArchivePostsDir);
            Progress.SetTotal(postFiles.Count());

            foreach (var postFile in postFiles)
            {
                Progress.Values[ParseProgressNames.CurrentFile] = postFile;
                Progress.Step();
                using (var postDoc = await _parser.FromFileAsync(postFile, Encoding.GetEncoding(1251), cancellationToken))
                {
                    var postDiv = postDoc.QuerySelector("div.singlePost");
                    if (postDiv == null)
                    {
                        continue;
                    }

                    postDiv.ClassList.Remove("countSecond");
                    postDiv.ClassList.Remove("countFirst");
                    postDiv.ClassList.Remove("lastPost");

                    var moreLinks = postDiv.QuerySelectorAll("a.LinkMore");
                    foreach (var moreLink in moreLinks)
                    {
                        moreLink.RemoveAttribute("onclick");
                        var linkId = moreLink.Id;
                        var spanId = linkId.Substring(4);
                        var spanEl = postDiv.QuerySelector($"#{spanId}");
                        if (spanEl == null)
                        {
                            continue;
                        }
                        var guid = Guid.NewGuid().ToString("n");
                        moreLink.Id = linkId + "_" + guid;
                        spanEl.Id = spanId + "_" + guid;
                    }

                    var newIndexDiv = indexDoc.CreateElement("div");
                    barDiv.Before(newIndexDiv);
                    newIndexDiv.OuterHtml = postDiv.OuterHtml;
                }
            }

            foreach (var script in indexDoc.QuerySelectorAll("script"))
            {
                script.Remove();
            }

            var scriptEl = indexDoc.CreateElement("script");
            scriptEl.SetAttribute("type", "text/javascript");
            scriptEl.SetAttribute("src", "./" + Constants.AccountPagesDir + "/jquery-3.2.1.min.js");
            indexDoc.QuerySelector("head").AppendChild(scriptEl);

            scriptEl = indexDoc.CreateElement("script");
            scriptEl.SetAttribute("type", "text/javascript");
            scriptEl.SetAttribute("src", "./" + Constants.AccountPagesDir + "/diary-scraper.js");
            indexDoc.QuerySelector("head").AppendChild(scriptEl);

            var linkRefFiles = _context.AccountPages.Where(p => !p.Url.Contains(".htm")).ToDictionary(p => p.Url, p => p.LocalPath);
            foreach (var link in indexDoc.QuerySelectorAll("link"))
            {
                if (!linkRefFiles.TryGetValue(link.GetAttribute("href"), out var localPath))
                {
                    continue;
                }
                var newUrl = localPath.Replace("\\", "/");
                newUrl = "./" + newUrl;
                link.SetAttribute("href", newUrl);
            }

            var imageFiles = _context.Images.ToDictionary(i => i.Url, i => "./" + i.RelativePath.Replace("\\", "/"));
            foreach (var img in indexDoc.QuerySelectorAll("img"))
            {
                img.RemoveAttribute("onload");
                if (!imageFiles.TryGetValue(img.GetAttribute("src"), out var localPath))
                {
                    continue;
                }
                img.SetAttribute("src", localPath);
            }

            indexDoc.WriteToFile(indexPath, Encoding.GetEncoding(1251));
        }

        private void CopyDir(string sourceDir, string destDir)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*",
                SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourceDir, "*.*",
                SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourceDir, destDir), true);
            }
        }

        public override void SetError(string error)
        {
            Progress.Error = error;
        }
    }
}