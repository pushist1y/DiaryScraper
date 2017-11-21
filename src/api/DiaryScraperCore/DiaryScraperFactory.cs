using System;
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
    public class DiaryScraperFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiaryScraperFactory> _logger;
        public DiaryScraperFactory(IServiceProvider serviceProvider, ILogger<DiaryScraperFactory> logger)
        {
            _serviceProvider = serviceProvider;
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
                    Overwrite = descriptor.Overwrite
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
                _logger.LogError("Error", e);
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
            var match = Regex.Match(diaryUrl, @"([\w-]+)\.diary\.ru");
            if (!match.Success)
            {
                throw new ArgumentException($"Неправильный адрес дневника: [{diaryUrl}]");
            }
            return match.Groups[1].Value;
        }


        private void UnsetLog(NLogScrapeConfig config)
        {
            try
            {
                if (config.Target != null)
                {
                    NLog.LogManager.Configuration.RemoveTarget(config.Target.Name);
                }
                if (config.Rule != null)
                {
                    NLog.LogManager.Configuration.LoggingRules.Remove(config.Rule);
                }
                NLog.LogManager.ReconfigExistingLoggers();
            }
            catch
            {
                //ignore exception
            }
        }
        private NLogScrapeConfig ConfigureLog(string workingDir)
        {
            try
            {
                var cfg = new NLogScrapeConfig();
                cfg.Target = new FileTarget();
                cfg.Target.Name = "errorTarget_" + Guid.NewGuid().ToString("n");
                cfg.Target.FileName = Path.Combine(workingDir, "${shortdate}.log");
                cfg.Target.Layout = @"${date:format=dd.MM.yyyy HH\:mm\:ss} (${level:uppercase=true}): ${message}. ${exception:format=ToString}";
                NLog.LogManager.Configuration.AddTarget(cfg.Target);

                cfg.Rule = new LoggingRule("DiaryScraperCore*", NLog.LogLevel.Warn, cfg.Target);
                NLog.LogManager.Configuration.LoggingRules.Add(cfg.Rule);

                NLog.LogManager.ReconfigExistingLoggers();
                return cfg;
            }
            catch
            {
                return null;
            }
        }

        private void EnsureDirs(string workingDir, string diaryName)
        {
            if (!Directory.Exists(workingDir))
            {
                throw new ArgumentException($"Директория [{workingDir}] не существует");
            }
            var diaryDir = Path.Combine(workingDir, diaryName);

            if (!Directory.Exists(diaryDir))
            {
                Directory.CreateDirectory(diaryDir);
            }

            var postDir = Path.Combine(diaryDir, Constants.PostsDir);
            if (!Directory.Exists(postDir))
            {
                Directory.CreateDirectory(postDir);
            }

            var imagesDir = Path.Combine(diaryDir, Constants.ImagesDir);
            if (!Directory.Exists(imagesDir))
            {
                Directory.CreateDirectory(imagesDir);
            }
        }
    }

    public class NLogScrapeConfig
    {
        public FileTarget Target { get; set; }
        public LoggingRule Rule { get; set; }
    }
}