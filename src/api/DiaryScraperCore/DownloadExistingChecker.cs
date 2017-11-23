using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{

    public class DownloadExistingChecker
    {

        private readonly ScrapeContext _context;
        private readonly ILogger _logger;
        private readonly string _diaryDir;

        public DownloadExistingChecker(string diaryDir, ScrapeContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
            _diaryDir = diaryDir;
        }

        public async Task<T> CheckUrlAsync<T>(string url, bool overwrite) where T : DownloadResource, new()
        {
            var array = new[] { url } as IEnumerable<string>;
            return (await FilterUrlsAsync<T>(array, overwrite)).FirstOrDefault();
        }

        private Dictionary<Type, object> _existingCache = new Dictionary<Type, object>();
        private Dictionary<string, T> GetExistingData<T>() where T : DownloadResource, new()
        {
            if (_existingCache.TryGetValue(typeof(T), out var cachedDict))
            {
                return cachedDict as Dictionary<string, T>;
            }

            var newDict = _context.Set<T>().ToDictionary(i => i.Url, i => i);
            _existingCache[typeof(T)] = newDict;
            return newDict;
        }

        public async Task<IList<T>> FilterUrlsAsync<T>(IEnumerable<string> urls, bool overwrite) where T : DownloadResource, new()
        {
            var downloadingImages = new List<T>();
            var existingData = GetExistingData<T>();

            foreach (var url in urls)
            {
                if (existingData.TryGetValue(url, out var dataProcessed))
                {
                    var filePath = Path.Combine(_diaryDir, dataProcessed.RelativePath);
                    if (File.Exists(filePath))
                    {
                        if (overwrite && !dataProcessed.JustCreated)
                        {
                            _logger.LogInformation($"Overwriting processed {typeof(T).Name}: " + url);
                            File.Delete(filePath);
                            _context.Set<T>().Remove(dataProcessed);
                            existingData.Remove(url);
                            downloadingImages.Add(new T{Url = url, JustCreated = true});
                        }
                        else
                        {
                            _logger.LogInformation($"Skipping processed {typeof(T).Name}: " + url);
                            continue;
                        }
                    }
                }
                else
                {
                    downloadingImages.Add(new T{Url = url, JustCreated = true});
                }
            }

            await _context.SaveChangesAsync();

            return downloadingImages;
        }


        public async Task AddProcessedDataAsync<T>(T dataItem) where T : DownloadResource, new()
        {
            var array = new T[] { dataItem } as IEnumerable<T>;
            await AddProcessedDataAsync(array);
        }

        public async Task AddProcessedDataAsync<T>(IEnumerable<T> dataList) where T : DownloadResource, new()
        {
            var existingData = GetExistingData<T>();
            await _context.Set<T>().AddRangeAsync(dataList);
            foreach (var data in dataList)
            {
                existingData[data.Url] = data;
            }
            await _context.SaveChangesAsync();
        }
    }
}