using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cloudflare_Bypass;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public class DataDownloader
    {
        public event EventHandler<DataDownloaderEventArgs> BeforeDownload;
        public event EventHandler<DataDownloaderEventArgs> AfterDownload;
        private readonly string _diaryPath;
        private readonly CookieContainer _cookieContainer;
        private readonly ILogger _logger;
        public DataDownloader(string diaryPath, CookieContainer cookieContainer, ILogger logger)
        {
            _diaryPath = diaryPath;
            _cookieContainer = cookieContainer;
            _logger = logger;
        }

        public async Task<DataDownloaderResult> Download(DownloadResource downloadResource, bool ignore404 = true, int requestDelay = 0)
        {
            if (downloadResource == null || string.IsNullOrEmpty(downloadResource.Url))
            {
                throw new ArgumentException("Для скачивания должны быть заполнены пути к данным");
            }
            _logger.LogInformation("Downloading data: " + downloadResource.Url);
            var uri = new Uri(downloadResource.Url);

            var filePath = string.IsNullOrEmpty(downloadResource.RelativePath)
                            ? string.Empty
                            : Path.Combine(_diaryPath, downloadResource.RelativePath);

            var client = new CF_WebClient(_cookieContainer);

            BeforeDownload?.Invoke(this, new DataDownloaderEventArgs { Resource = downloadResource });
            Thread.Sleep(requestDelay);
            byte[] downloadedData;
            var retries = 0;
            while (true)
            {
                try
                {
                    
                    downloadedData = await client.DownloadDataTaskAsync(uri);
                    break; //i want to break freeeeee
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
                                downloadResource.LocalPath = "";
                                return new DataDownloaderResult { Resource = downloadResource, DownloadedData = null };
                            }
                        }
                    }
                    retries += 1;
                    _logger.LogError(e, $"Error, retry count: {retries}");

                    if (retries >= Constants.DownloadRetryCount)
                    {
                        throw;
                    }
                    Thread.Sleep(2000);
                }
            }

            AfterDownload?.Invoke(this, new DataDownloaderEventArgs { Resource = downloadResource, DownloadedData = downloadedData });

            if (!string.IsNullOrEmpty(filePath))
            {
                using (var f = File.Create(filePath))
                {
                    await f.WriteAsync(downloadedData, 0, downloadedData.Length);
                }
            }

            return new DataDownloaderResult { Resource = downloadResource, DownloadedData = downloadedData };

        }

        public async Task<IList<DataDownloaderResult>> Download(IEnumerable<DownloadResource> dataList, bool ignore404 = true, int requestDelay = 0)
        {
            var list = new List<DataDownloaderResult>();
            var tasks = dataList.Select(item => Download(item, ignore404, requestDelay));
            foreach (var task in tasks)
            {
                var img = await task;
                list.Add(img);
            }
            return list;
        }

    }

    public class DataDownloaderResult
    {
        public DownloadResource Resource { get; set; }
        public byte[] DownloadedData { get; set; }
    }

    public class DataDownloaderEventArgs : DataDownloaderResult
    {

    }

}