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
using AngleSharp.Dom.Html;
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

        protected async Task<bool> FixVoting(IHtmlDocument doc)
        {
            var votingDiv = doc.QuerySelector("div.voting");
            if (votingDiv == null)
            {
                return false;
            }

            var linkElement = votingDiv.QuerySelector("a[id*='poll']");
            if (linkElement == null)
            {
                return false;
            }

            var signatureElement = votingDiv.QuerySelector("input[name='signature']");
            var signature = signatureElement.GetAttribute("value");

            var url = linkElement.GetAttribute("href");
            url += "&js&signature=" + signature;
            var resource = new DownloadResource() { Url = url };
            var res = await _dataDownloader.Download(resource);
            var resString = res.DownloadedData.AsAnsiString();
            var match = Regex.Match(resString, @"get\('(\w+)'\)\.innerHTML\s+=\s+'([^']*)'");
            if (!match.Success)
            {
                return false;
            }

            var divId = match.Groups[1].Value;
            var newHtml = match.Groups[2].Value.Replace(@"\""", @"""");
            var replaceDiv = doc.QuerySelector($"#{divId}");
            if (replaceDiv == null)
            {
                return false;
            }
            replaceDiv.InnerHtml = newHtml;
            votingDiv.QuerySelector("span[id*='spanpollaction']")?.Remove();

            return true;
        }

        public async Task<bool> FixPage(IHtmlDocument doc)
        {
            var rewrite = false;
            rewrite = rewrite || await FixMore(doc);
            rewrite = rewrite || await FixVoting(doc);
            return rewrite;

        }
        protected async Task<bool> FixMore(IHtmlDocument doc)
        {
            await DetectMoreType();
            if (this._moreType == DiaryMoreLinksType.Preloaded)
            {
                return false;
            }

            var moreLinks = doc.QuerySelectorAll("a.LinkMore");

            var actualLinks = (from moreLink in moreLinks
                               let href = moreLink.GetAttribute("href")
                               where !string.IsNullOrEmpty(href) && href.ToLower() != "#more"
                               select moreLink).ToList();

            if (actualLinks.Count <= 0)
            {
                return false;
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

            return true;
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
