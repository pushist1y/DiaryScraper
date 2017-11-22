using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{

    public class DownloadExistingChecker<T> where T : DownloadResource, new()
    {

        private Dictionary<string, T> _existingData = new Dictionary<string, T>();
        private readonly ScrapeContext _context;
        private readonly ILogger _logger;
        private readonly string _diaryDir;

        public DownloadExistingChecker(string diaryDir, ScrapeContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
            _diaryDir = diaryDir;
        }

        private string _dataName => (typeof(T) == typeof(DiaryPost)) ? "post" : "image";
        private string _directoryName => (typeof(T) == typeof(DiaryPost)) ? "posts" : "images";

        public void InitializeFromContext()
        {
            _existingData = _context.Set<T>().ToDictionary(i => i.Url, i => i);
        }

        public async Task<T> CheckUrl(string url, bool overwrite)
        {
            return (await FilterUrls(new[] { url }, overwrite)).FirstOrDefault();
        }

        public async Task<IList<T>> FilterUrls(IEnumerable<string> urls, bool overwrite)
        {
            var downloadingImages = new List<T>();

            foreach (var url in urls)
            {
                if (_existingData.TryGetValue(url, out var dataProcessed))
                {
                    var filePath = Path.Combine(_diaryDir, dataProcessed.RelativePath);
                    if (File.Exists(filePath))
                    {
                        if (overwrite && !dataProcessed.JustCreated)
                        {
                            _logger.LogInformation($"Overwriting processed {_dataName}: " + url);
                            File.Delete(filePath);
                            _context.Set<T>().Remove(dataProcessed);
                            _existingData.Remove(url);
                            downloadingImages.Add(new T() { Url = url, JustCreated = true });
                        }
                        else
                        {
                            _logger.LogInformation($"Skipping processed {_dataName}: " + url);
                            continue;
                        }
                    }
                }
                else
                {
                    downloadingImages.Add(new T() { Url = url, JustCreated = true });
                }
            }

            await _context.SaveChangesAsync();

            return downloadingImages;
        }


        public async Task AddProcessedData(T dataItem)
        {
            await AddProcessedData(new T[] { dataItem });
        }
        
        public async Task AddProcessedData(IEnumerable<T> dataList)
        {
            await _context.Set<T>().AddRangeAsync(dataList);
            foreach (var data in dataList)
            {
                _existingData[data.Url] = data;
            }
            await _context.SaveChangesAsync();
        }
    }
}