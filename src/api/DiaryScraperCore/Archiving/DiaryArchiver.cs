using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

            var postStrings = new List<string>();

            foreach (var postFile in postFiles)
            {
                Progress.Values[ParseProgressNames.CurrentFile] = postFile;
                Progress.Step();
                using (var postDoc = await _parser.FromFileAsync(postFile, Encoding.GetEncoding(1251), cancellationToken))
                {
                    var sourcePostDiv = postDoc.QuerySelector("div.singlePost");
                    if (sourcePostDiv == null)
                    {
                        continue;
                    }

                    //ReplaceDiaryMediaIframes(postDoc);

                    var moreLinks = postDoc.QuerySelectorAll("a.LinkMore");
                    foreach (var moreLink in moreLinks)
                    {
                        moreLink.RemoveAttribute("onclick");
                        var linkId = moreLink.Id;
                        var spanId = linkId.Substring(4);
                        var spanEl = postDoc.QuerySelector($"#{spanId}");
                        if (spanEl == null)
                        {
                            continue;
                        }
                        var guid = Guid.NewGuid().ToString("n");
                        moreLink.Id = linkId + "_" + guid;
                        spanEl.Id = spanId + "_" + guid;
                    }

                    AddScripts(postDoc, "../");
                    ReplaceImageSources(postDoc, "../");
                    ReplaceLinkRef(postDoc, "../");

                    var newEl = indexDoc.CreateElement("div");
                    newEl.InnerHtml = sourcePostDiv.OuterHtml;
                    var postDiv = newEl.QuerySelector("div");
                    //barDiv.After(postDiv);

                    postDiv.ClassList.Remove("countSecond");
                    postDiv.ClassList.Remove("countFirst");
                    postDiv.ClassList.Remove("lastPost");

                    postDiv.QuerySelector("div.postLinksBackg.prevnext")?.Remove();
                    var postLink = "./" + Constants.PostsDir + "/" + Path.GetFileName(postFile);
                    var ul = postDiv.QuerySelector("ul.postLinks");
                    if (ul != null)
                    {
                        var commentCount = postDoc.QuerySelectorAll(".singleComment").Count();
                        ul.InnerHtml = $"<li class=\"comments\"><a href=\"{postLink}\"><span>Комментарии</span></a> <span class=\"comments_count_link\">(<a href=\"{postLink}\">{commentCount}</a>)</span></li>";
                    }

                    postDiv.QuerySelector(".postLinksBackg .urlLink a")?.SetAttribute("href", postLink);
                    
                    postDoc.QuerySelector("#addCommentArea")?.Remove();
                    

                    postStrings.Add(postDiv.OuterHtml);

                    postDoc.WriteToFile(postFile, Encoding.GetEncoding(1251));
                }
            }

            AddScripts(indexDoc, "./");
            ReplaceLinkRef(indexDoc, "./");
            ReplaceImageSources(indexDoc, "./");

            SetNav(indexDoc, postStrings);

            indexDoc.WriteToFile(indexPath, Encoding.GetEncoding(1251));
        }


        private Dictionary<string, string> _linkRefFiles = null;
        private void ReplaceLinkRef(IHtmlDocument doc, string dirPrefix)
        {
            if (_linkRefFiles == null)
            {
                _linkRefFiles = _context.AccountPages.Where(p => !p.Url.Contains(".htm")).ToDictionary(p => p.Url, p => p.LocalPath);
            }
            foreach (var link in doc.QuerySelectorAll("link"))
            {
                if (!_linkRefFiles.TryGetValue(link.GetAttribute("href"), out var localPath))
                {
                    continue;
                }
                localPath = localPath.Replace("\\", "/");
                localPath = dirPrefix + localPath;
                link.SetAttribute("href", localPath);
            }
        }

        private void ReplaceDiaryMediaIframes(IHtmlDocument doc)
        {
            var frames = (from frame in  doc.QuerySelectorAll("iframe")
            let src = frame.GetAttribute("src")
            where !string.IsNullOrEmpty(src) && src.Contains("diary-media.ru")
            select frame).ToList();
            
            foreach(var frame in frames)
            {
                var src = frame.GetAttribute("src");
                Console.WriteLine(src);
                var match = Regex.Match(src, @"diary-media\.ru\/\?(.*)$");
                if(!match.Success)
                {
                    continue;
                }
                var base64Part = match.Groups[1].Value;
                var decodedData = Convert.FromBase64String(base64Part);
                var decodedString = Encoding.UTF8.GetString(decodedData);
                Console.WriteLine(decodedString);
            }
            
        }

        private void AddScripts(IHtmlDocument doc, string dirPrefix)
        {
            foreach (var script in doc.QuerySelectorAll("script"))
            {
                script.Remove();
            }

            var scriptElPost = doc.CreateElement("script");
            scriptElPost.SetAttribute("type", "text/javascript");
            scriptElPost.SetAttribute("src", dirPrefix + Constants.AccountPagesDir + "/" + Constants.JQueryFileName);
            doc.QuerySelector("head").AppendChild(scriptElPost);

            scriptElPost = doc.CreateElement("script");
            scriptElPost.SetAttribute("type", "text/javascript");
            scriptElPost.SetAttribute("src", dirPrefix + Constants.AccountPagesDir + "/" + Constants.DiaryJsFileName);
            doc.QuerySelector("head").AppendChild(scriptElPost);
        }

        private Dictionary<string, string> _imageFiles = null;
        private void ReplaceImageSources(IHtmlDocument doc, string dirPrefix)
        {
            if (_imageFiles == null)
            {
                _imageFiles = _context.Images
                                .Where(i => !string.IsNullOrEmpty(i.RelativePath))
                                .ToDictionary(i => i.Url, i => i.RelativePath);
                var fav = _context.AccountPages.FirstOrDefault(ap => ap.Url.Contains("favicon.ico"));
                if (fav != null)
                {
                    _imageFiles[fav.Url] = fav.RelativePath;
                }
            }
            foreach (var img in doc.QuerySelectorAll("img"))
            {
                if(!img.HasAttribute("src"))
                {
                    continue;
                }
                img.RemoveAttribute("onload");
                if (!_imageFiles.TryGetValue(img.GetAttribute("src"), out var imagePath))
                {
                    continue;
                }
                imagePath = imagePath.Replace("\\", "/");
                imagePath = dirPrefix + imagePath;
                img.SetAttribute("src", imagePath);
            }
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

        private void SetNav(IHtmlDocument indexDoc, IEnumerable<string> postStrings)
        {
            var tds = indexDoc.QuerySelectorAll("#pageBar tr.pages_str td");
            var tdPrev = tds.First();
            var tdNext = tds.Last();

            var anchorPrev = indexDoc.CreateElement("a");
            anchorPrev.TextContent = "< предыдущая";
            anchorPrev.SetAttribute("href", "#");
            anchorPrev.Id = "anchorPrev";
            tdPrev.InnerHtml = "";
            tdPrev.AppendChild(anchorPrev);

            var anchorNext = indexDoc.CreateElement("a");
            anchorNext.TextContent = "следующая >";
            anchorNext.SetAttribute("href", "#");
            anchorNext.Id = "anchorNext";
            tdNext.InnerHtml = "";
            tdNext.AppendChild(anchorNext);

            var td = indexDoc.QuerySelectorAll("#pageBar tr").Last().QuerySelector("td");
            td.InnerHtml = "";
            td.Id = "tdPages";
            // var pageCount = Convert.ToInt32(Math.Ceiling(1.0 * indexDoc.QuerySelectorAll("div.singlePost").Count() / Constants.ArchivePageSize));
            // for (var i = 1; i <= pageCount; i++)
            // {
            //     var pageAnchor = indexDoc.CreateElement(i == 1 ? "strong" : "a");
            //     pageAnchor.InnerHtml = Convert.ToString(i);
            //     pageAnchor.SetAttribute("href", $"#{i}");
            //     pageAnchor.ClassList.Add("pageAnchor");
            //     pageAnchor.SetAttribute("page", i.ToString());
            //     td.AppendChild(pageAnchor);
            // }

            var serializedData = JsonConvert.SerializeObject(postStrings.Reverse(), new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            });

            var script = indexDoc.CreateElement("script");
            script.SetAttribute("type", "text/javascript");
            script.InnerHtml = $@"
            $(function(){{
                postStrings = {serializedData};
                initPages({Constants.ArchivePageSize});
            }});
                
            ";
            indexDoc.QuerySelector("head").AppendChild(script);

        }

        public override void SetError(string error)
        {
            Progress.Error = error;
        }
    }
}