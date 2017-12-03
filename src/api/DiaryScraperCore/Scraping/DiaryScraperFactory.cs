using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Targets;

namespace DiaryScraperCore
{
    public class DiaryScraperFactory : WorkerFactoryBase
    {

        private readonly ILogger<DiaryScraperFactory> _logger;
        public DiaryScraperFactory(IServiceProvider serviceProvider, ILogger<DiaryScraperFactory> logger) : base(serviceProvider)
        {
            _logger = logger;
        }

        public DiaryScraperNew GetScraper(ScrapeTaskDescriptor descriptor, string login, string password)
        {
            try
            {
                if (descriptor.ScrapeStart > descriptor.ScrapeEnd)
                {
                    throw new ArgumentException("Неверный интервал дат");
                }
                var diaryName = GetDiaryName(descriptor.DiaryUrl);

                EnsureDirs(descriptor.WorkingDir, diaryName);

                var cfg = ConfigureLog(descriptor.WorkingDir);
                var logger = _serviceProvider.GetRequiredService<ILogger<DiaryScraperNew>>();
                var context = GetContext(descriptor.WorkingDir, diaryName);
                var options = new DiaryScraperOptions
                {
                    WorkingDir = descriptor.WorkingDir,
                    DiaryName = diaryName,
                    Login = login,
                    Password = password,
                    RequestDelay = descriptor.RequestDelay,
                    ScrapeStart = descriptor.ScrapeStart,
                    ScrapeEnd = descriptor.ScrapeEnd,
                    Overwrite = descriptor.Overwrite,
                    DownloadEdits = descriptor.DownloadEdits,
                    DownloadAccount = descriptor.DownloadAccount
                };

                var scraper = new DiaryScraperNew(logger, context, options);

                scraper.ScrapeFinished += (s, e) =>
                {
                    UnsetLog(cfg);
                };

                descriptor.Scraper = scraper;
                return scraper;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error");
                descriptor.SetError(e.Message);
                return null;
            }
        }

        private ScrapeContext GetContext(string workingDir, string diaryName)
        {
            var dbPath = Path.Combine(workingDir, diaryName, Constants.DbName);
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite($@"Data Source={dbPath}");
            var context = new ScrapeContext(optionsBuilder.Options);
            context.Database.Migrate();

            return context;
        }

        private string GetDiaryName(string diaryUrl)
        {
            var match = Regex.Match(diaryUrl, @"([\w-]+)\.diary\.ru", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                throw new ArgumentException($"Неправильный адрес дневника: [{diaryUrl}]");
            }
            return match.Groups[1].Value;
        }




        private void EnsureDirs(string workingDir, string diaryName)
        {
            if (!Directory.Exists(workingDir))
            {
                throw new ArgumentException($"Директория [{workingDir}] не существует");
            }
            var dirs = new List<string>();
            var diaryDir = Path.Combine(workingDir, diaryName);
            dirs.Add(diaryDir);
            dirs.Add(Path.Combine(diaryDir, Constants.PostsDir));
            dirs.Add(Path.Combine(diaryDir, Constants.ImagesDir));
            dirs.Add(Path.Combine(diaryDir, Constants.PostEditsDir));
            dirs.Add(Path.Combine(diaryDir, Constants.AccountPagesDir));

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            
        }
    }

    
}