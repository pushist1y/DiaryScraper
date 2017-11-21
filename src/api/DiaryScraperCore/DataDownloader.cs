using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cloudflare_Bypass;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{

    public class DataDownloader
    {
        private readonly string _diaryPath;
        private readonly CookieContainer _cookieContainer;
        private readonly ILogger _logger;
        private readonly ScrapeTaskProgress _progress;
        public DataDownloader(string diaryPath, CookieContainer cookieContainer, ILogger logger, ScrapeTaskProgress progress)
        {
            _diaryPath = diaryPath;
            _cookieContainer = cookieContainer;
            _logger = logger;
            _progress = progress;
        }


        public async Task<DownloadResource> Download(DownloadResource data, bool getData = false, bool ignore404 = true, int requestDelay = 0)
        {
            if (data == null || string.IsNullOrEmpty(data.Url) || string.IsNullOrEmpty(data.RelativePath))
            {
                throw new ArgumentException("Для скачивания должны быть заполнены пути к данным");
            }
            _logger.LogInformation("Downloading data: " + data.Url);
            _progress.CurrentUrl = data.Url;
            var uri = new Uri(data.Url);
            var filePath = Path.Combine(_diaryPath, data.RelativePath);
            var client = new CF_WebClient(_cookieContainer);
            try
            {
                var downloadedData = await client.DownloadDataTaskAsync(uri);

                if (data is DiaryPost)
                {
                    _progress.PageDownloaded(downloadedData);
                }
                else if (data is DiaryImage)
                {
                    _progress.ImageDownloaded(downloadedData);
                }

                if (getData)
                {
                    data.Data = downloadedData;
                }

                using (var f = File.Create(filePath))
                {
                    await f.WriteAsync(downloadedData, 0, downloadedData.Length);
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError && ignore404)
                {
                    var response = e.Response as HttpWebResponse;
                    if (response != null)
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            _logger.LogWarning("Url not found: " + e.Response.ResponseUri.AbsoluteUri);
                            return null;
                        }
                    }
                }
            }
            return data;
        }

        public async Task<IList<DownloadResource>> Download(IEnumerable<DownloadResource> dataList, bool getData = false, bool ignore404 = true, int requestDelay = 0)
        {
            var list = new List<DownloadResource>();
            var tasks = dataList.Select(item => Download(item, ignore404, getData));
            foreach (var task in tasks)
            {
                var img = await task;
                if (img != null)
                {
                    list.Add(img);
                }
            }
            return list;
        }

    }


}