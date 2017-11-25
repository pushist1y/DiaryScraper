using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using AngleSharp.Xml;

namespace DiaryScraperCore
{
    public class DiaryMoreLinksFixer
    {
        private readonly DataDownloader _dataDownloader;
        private DiaryMoreLinksType _moreType = DiaryMoreLinksType.Undefined;
        private readonly HtmlParser _parser;
        private readonly string _diaryName;
        private readonly string _diaryDir;
        public DiaryMoreLinksFixer(DataDownloader dataDownloader, string workingDir, string diaryName)
        {
            _dataDownloader = dataDownloader;
            var config = new Configuration().WithCss();
            _parser = new HtmlParser(config);
            _diaryName = diaryName;
            _diaryDir = Path.Combine(workingDir, diaryName);
        }
        public async Task FixMore(DataDownloaderResult downloadedDiaryPost)
        {
            await DetectMoreType();
            if (this._moreType == DiaryMoreLinksType.Preloaded)
            {
                return;
            }
            if (string.IsNullOrEmpty(downloadedDiaryPost.Resource.RelativePath))
            {
                return;
            }


            var src = downloadedDiaryPost.DownloadedData.AsAnsiString();
            var doc = _parser.Parse(src);
            var moreLinks = doc.QuerySelectorAll("a.LinkMore");

            var actualLinks = (from moreLink in moreLinks
                               let href = moreLink.GetAttribute("href")
                               where !string.IsNullOrEmpty(href) && href.ToLower() != "#more"
                               select moreLink).ToList();

            if (actualLinks.Count <= 0)
            {
                return;
            }

            if (_moreType == DiaryMoreLinksType.OnDemand)
            {
                var dataToLoad = (from link in actualLinks
                                  let matches = Regex.Matches(link.GetAttribute("onclick"), @"\""([^\""]*)\""")
                                  where matches.Count > 1
                                  select new
                                  {
                                      LinkElement = link,
                                      Url = $"http://{_diaryName}.diary.ru{matches[1].Groups[1].Value}?post={matches[0].Groups[1].Value}&js",
                                      MorePartName = matches[0].Groups[1].Value
                                  }
                ).ToList();

                var resources = dataToLoad.Select(d => new DownloadResource { Url = d.Url });
                var downloadResults = await _dataDownloader.Download(resources);

                var results = (from d in dataToLoad
                               from r in downloadResults
                               where d.Url == r.Resource.Url
                               select new { d.LinkElement, d.Url, r.DownloadedData, d.MorePartName })
                .ToList();

                foreach (var r in results)
                {
                    var match = Regex.Match(r.DownloadedData.AsAnsiString(), @"innerHTML\s*=\s*'([^']*)'");
                    if (!match.Success)
                    {
                        continue;
                    }
                    var htmlText = match.Groups[1].Value;
                    var spanId = $"more{r.MorePartName}";
                    var spanElement = doc.QuerySelector($"#{spanId}");
                    if (spanElement == null)
                    {
                        continue;
                    }
                    spanElement.InnerHtml = htmlText;
                }
            }
            else if (_moreType == DiaryMoreLinksType.FullPage)
            {
                var resource = new DownloadResource { Url = actualLinks[0].GetAttribute("href") };
                var downloadResult = await _dataDownloader.Download(resource, false, 1000);
                var docFull = await _parser.ParseAsync(downloadResult.DownloadedData.AsAnsiString());
                foreach (var link in actualLinks)
                {
                    var match = Regex.Match(link.GetAttribute("href"), @"\/p(\d*).html?\?oam#(.*)$");
                    if (!match.Success)
                    {
                        continue;
                    }
                    var postNum = match.Groups[1].Value;
                    var moreName = match.Groups[2].Value;
                    var elementStart = docFull.QuerySelector($"a[name='{moreName}']");
                    var elementEnd = docFull.QuerySelector($"a[name='{moreName}end']");
                    if (elementStart == null || elementEnd == null)
                    {
                        continue;
                    }
                    var newDiv = docFull.CreateElement("div");
                    elementStart.Before(newDiv);
                    var nodesToCopy = new List<INode>();
                    var currentNode = elementStart.NextSibling;
                    while (currentNode != null)
                    {
                        if (currentNode == elementEnd)
                        {
                            break;
                        }
                        nodesToCopy.Add(currentNode);
                        currentNode = currentNode.NextSibling;
                    }

                    foreach (var el in nodesToCopy)
                    {
                        newDiv.AppendChild(el);
                    }

                    var moreHtml = newDiv.InnerHtml;

                    var moreSpanId = "more" + postNum + "m" + moreName.Substring(4);
                    var newMoreSpan = doc.CreateElement("span");
                    newMoreSpan.Id = moreSpanId;

                    newMoreSpan.Style.Display = "none";
                    newMoreSpan.Style.Visibility = "hidden";
                    link.After(newMoreSpan);
                    link.Id = "link" + moreSpanId;
                    newMoreSpan.InnerHtml = moreHtml;
                }
            }

            var filePath = Path.Combine(_diaryDir, downloadedDiaryPost.Resource.RelativePath);
            using (var sw = new StreamWriter(File.Open(filePath, FileMode.Create), Encoding.GetEncoding(1251)))
            {
                doc.ToHtml(sw, XmlMarkupFormatter.Instance);
            }
        }

        private async Task DetectMoreType()
        {
            if (this._moreType != DiaryMoreLinksType.Undefined)
            {
                return;
            }
            var resource = new DownloadResource { Url = "http://www.diary.ru/options/site/?msgtags" };
            var optionsPageData = await _dataDownloader.Download(resource, false, 1000);
            var doc = _parser.Parse(optionsPageData.DownloadedData.AsAnsiString());
            var element = doc.QuerySelector("input[type='radio'][name='more_type']:checked");
            _moreType = (DiaryMoreLinksType)Convert.ToInt32(element.GetAttribute("value"));

        }
    }

    public enum DiaryMoreLinksType
    {
        Undefined = -1,
        Preloaded = 0,
        OnDemand = 2,
        FullPage = 1
    }
}