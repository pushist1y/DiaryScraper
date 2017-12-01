using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class DiaryParserEventArgs { }

    public class DiaryParserOptions
    {
        public string DiaryDir { get; set; }
    }
    public class DiaryParser
    {
        public Task Worker { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
        public DiaryParserProgress Progress { get; set; } = new DiaryParserProgress();
        public event EventHandler<ScraperFinishedArguments> ParseFinished;

        private readonly DiaryParserOptions _options;
        private readonly ILogger<DiaryParser> _logger;
        private readonly HtmlParser _parser;
        public DiaryParser(DiaryParserOptions options, ILogger<DiaryParser> logger)
        {
            _options = options;
            _logger = logger;
            var config = new Configuration().WithCss();
            _parser = new HtmlParser(config);
            TokenSource = new CancellationTokenSource();
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
                ParseFinished?.Invoke(this, new ScraperFinishedArguments());
            }
        }

        private string _parsedDir;
        public void DoWork(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(_options.DiaryDir))
            {
                throw new ArgumentException($"Директория [{_options.DiaryDir}] не существует");
            }

            var dbFilePath = Path.Combine(_options.DiaryDir, Constants.DbName);
            if (!File.Exists(dbFilePath))
            {
                throw new ArgumentException($"В папке [{_options.DiaryDir}] отсутствует файл {Constants.DbName}");
            }

            var postsDir = Path.Combine(_options.DiaryDir, "posts");
            if (!Directory.Exists(postsDir))
            {
                throw new ArgumentException($"Директория [{postsDir}] не существует");
            }

            _parsedDir = Path.Combine(_options.DiaryDir, "parsed");
            if (!Directory.Exists(_parsedDir))
            {
                Directory.CreateDirectory(_parsedDir);
            }

            var filePaths = new List<string>();
            filePaths.AddRange(Directory.GetFiles(postsDir));
            Progress.Values[ParseProgressNames.PostsDiscovered] = filePaths.Count;
            Progress.RangeDiscovered = true;
            var i = 0;
            while (filePaths.Count > 0)
            {
                var portion = filePaths.Take(20).ToList();
                filePaths = filePaths.Skip(20).ToList();
                ParsePostsPortion(i++, portion, cancellationToken).Wait();
            }

        }

        private async Task ParsePostsPortion(int portionIndex, IEnumerable<string> filePaths, CancellationToken cancellationToken)
        {
            var filePath = $"posts_{portionIndex:D5}.json";
            filePath = Path.Combine(_parsedDir, filePath);

            var posts = new List<DiaryPostDto>();
            foreach (var fPath in filePaths)
            {
                var editPath = Regex.Replace(fPath, @"([\\\/])posts([\\\/])", "$1postedits$2");
                editPath = Regex.Replace(editPath, @"\.htm$", "_edit.htm");

                Progress.Values[ParseProgressNames.CurrentFile] = Path.GetFileName(fPath);
                var postDto = new DiaryPostDto();
                IHtmlDocument doc, docEdit = null;
                using (var sr = new StreamReader(File.Open(fPath, FileMode.Open), Encoding.GetEncoding(1251)))
                {
                    doc = await _parser.ParseAsync(sr.BaseStream, cancellationToken);
                }
                if (File.Exists(editPath))
                {
                    using (var sr = new StreamReader(File.Open(editPath, FileMode.Open), Encoding.GetEncoding(1251)))
                    {
                        docEdit = await _parser.ParseAsync(sr.BaseStream, cancellationToken);
                    }
                }

                var postDiv = doc.QuerySelector("div.singlePost");
                if (postDiv == null)
                {
                    Console.WriteLine("Hidden post: " + fPath);
                    Progress.IncrementInt(ParseProgressNames.PostsProcessed, 1);
                    continue;
                }

                postDto.PostId = Regex.Match(postDiv.Id, @"(\d+)").Groups[1].Value;

                var dateline1 = postDiv.QuerySelector(".countSecondDate.postDate span").TextContent;
                var dateline2 = postDiv.QuerySelector(".postTitle.header span").TextContent;
                postDto.DatelineDate = dateline1 + ", " + dateline2;
                postDto.DatelistCdate = postDto.DatelineDate;

                var moreLinks = doc.QuerySelectorAll("a.LinkMore");
                foreach (var link in moreLinks)
                {
                    var linkId = link.Id;
                    var moreSpanId = linkId.Replace("link", "");
                    var spanElement = doc.QuerySelector($"#{moreSpanId}");
                    if (spanElement == null)
                    {
                        continue;
                    }
                    var moreText = link.TextContent;
                    var replacementHtml = $"[MORE={moreText}]{spanElement.InnerHtml}[/MORE]";
                    link.Remove();
                    spanElement.OuterHtml = replacementHtml;
                }

                var brs = doc.QuerySelectorAll(".postInner .paragraph div br");
                foreach (var br in brs)
                {
                    br.OuterHtml = "\n";
                }

                var anchors = doc.QuerySelectorAll("a[name*='more']").Where(a => !a.HasAttribute("href"));
                foreach (var a in anchors)
                {
                    a.Remove();
                }

                if (docEdit != null)
                {
                    postDto.MessageHtml = docEdit.QuerySelector("textarea#message").TextContent;
                }
                else
                {
                    postDto.MessageHtml = postDiv.QuerySelector(".postInner .paragraph div").InnerHtml;
                }
                postDto.Title = postDiv.QuerySelector(".postTitle.header h2").TextContent;
                postDto.AuthorUsername = postDiv.QuerySelector("div.authorName a strong").TextContent;
                postDto.CurrentMusic = postDiv.QuerySelector("p.atMusic em span")?.NextSibling?.TextContent?.Trim() ?? string.Empty;
                postDto.CurrentMood = postDiv.QuerySelector("p.atMood em span")?.NextSibling?.TextContent?.Trim() ?? string.Empty;
                postDto.Tags.AddRange(postDiv.QuerySelectorAll("p.atTag a").Select(e => e.TextContent));

                if (docEdit != null)
                {
                    var checkedAccess = docEdit.QuerySelector("input[type='radio'][id*='closeaccessmode']:checked");
                    postDto.Access = (checkedAccess == null) ? "0" : checkedAccess.GetAttribute("value");

                    var checkNoComment = docEdit.QuerySelector("input#nocomm");
                    postDto.NoComments = (checkNoComment == null || !checkNoComment.HasAttribute("checked")) ? "0" : "1";

                    var accessListText = docEdit.QuerySelector("textarea#access_list2").TextContent;
                    if (!string.IsNullOrEmpty(accessListText))
                    {
                        postDto.AccessList = accessListText.Split("\n").ToList();
                    }
                }
                else
                {
                    var lockImg = postDiv.QuerySelector(".postTitle h2 img[alt='lock']");
                    postDto.Access = (lockImg == null) ? "0" : "7";

                    var subscribeEl = doc.QuerySelector("li.subscribe");
                    postDto.NoComments = (subscribeEl == null) ? "1" : "0";
                }


                var commentElements = doc.QuerySelectorAll("div.singleComment");
                foreach (var commentElement in commentElements)
                {
                    var commentDto = new DiaryCommentDto();
                    commentDto.Dateline = commentElement.QuerySelector(".postTitle.header span").TextContent;
                    commentDto.AuthorUsername = commentElement.QuerySelector(".authorName a strong").TextContent;
                    commentDto.MessageHtml = commentElement.QuerySelector(".postInner .paragraph div").InnerHtml;
                    postDto.Comments.Add(commentDto);
                }
                posts.Add(postDto);
                Progress.IncrementInt(ParseProgressNames.PostsProcessed, 1);
            }

            var jsonStr = JsonConvert.SerializeObject(posts, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            });

            using (var f = File.CreateText(filePath))
            {
                await f.WriteLineAsync(jsonStr);
            }
        }

    }

    public class DiaryPostDto
    {
        [JsonProperty("author_username")]
        public string AuthorUsername { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        [JsonProperty("no_comments")]
        public string NoComments { get; set; }
        public string Access { get; set; }
        public string Title { get; set; }
        [JsonProperty("message_html")]
        public string MessageHtml { get; set; }
        [JsonProperty("dateline_date")]
        public string DatelineDate { get; set; }
        [JsonProperty("dateline_cdate")]
        public string DatelistCdate { get; set; }
        [JsonProperty("postid")]
        public string PostId { get; set; }

        [JsonProperty("access_list")]
        public List<string> AccessList { get; set; } = new List<string>();
        [JsonProperty("current_mood")]
        public string CurrentMood { get; set; }
        [JsonProperty("current_music")]
        public string CurrentMusic { get; set; }

        public List<DiaryCommentDto> Comments { get; set; } = new List<DiaryCommentDto>();
    }

    public class DiaryCommentDto
    {
        [JsonProperty("author_username")]
        public string AuthorUsername { get; set; }
        public string Dateline { get; set; }
        [JsonProperty("message_html")]
        public string MessageHtml { get; set; }
    }
}