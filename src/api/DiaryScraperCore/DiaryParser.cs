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
            var filePath = $"posts_{portionIndex:D3}.json";
            filePath = Path.Combine(_parsedDir, filePath);

            var posts = new List<DiaryPostDto>();
            foreach (var fPath in filePaths)
            {
                Progress.Values[ParseProgressNames.CurrentFile] = Path.GetFileName(fPath);
                var postDto = new DiaryPostDto();
                IHtmlDocument doc;
                using (var sr = new StreamReader(File.Open(fPath, FileMode.Open), Encoding.GetEncoding(1251)))
                {
                    doc = await _parser.ParseAsync(sr.BaseStream, cancellationToken);
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
                postDto.MessageHtml = postDiv.QuerySelector(".postInner .paragraph div").InnerHtml;
                postDto.Title = postDiv.QuerySelector(".postTitle.header h2").TextContent;

                postDto.Tags.AddRange(postDiv.QuerySelectorAll("p.atTag a").Select(e => e.TextContent));

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
        public List<string> Tags { get; set; } = new List<string>();
        [JsonProperty("no_comments")]
        public byte NoComments { get; set; }
        public byte Access { get; set; }
        public string Title { get; set; }
        [JsonProperty("message_html")]
        public string MessageHtml { get; set; }
        [JsonProperty("dateline_date")]
        public string DatelineDate { get; set; }
        [JsonProperty("dateline_cdate")]
        public string DatelistCdate { get; set; }
        [JsonProperty("postid")]
        public string PostId { get; set; }


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