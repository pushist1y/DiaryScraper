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
    public class WorkerFactoryBase
    {
        protected readonly IServiceProvider _serviceProvider;
        public WorkerFactoryBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected void UnsetLog(NLogScrapeConfig config)
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
        protected NLogScrapeConfig ConfigureLog(string workingDir)
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
    }
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
                    DownloadEdits = descriptor.DownloadEdits
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

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            
        }
    }

    public class DiaryParserFactory : WorkerFactoryBase
    {
        private readonly ILogger<DiaryParser> _logger;
        public DiaryParserFactory(IServiceProvider serviceProvider, ILogger<DiaryParser> logger) : base(serviceProvider)
        {
            _logger = logger;
        }

        public DiaryParser GetParser(ParseTaskDescriptor descriptor)
        {
            var options = new DiaryParserOptions();
            options.DiaryDir = descriptor.WorkingDir;

            descriptor.Parser = new DiaryParser(options, _logger);

            return descriptor.Parser;
        }
    }

    public class NLogScrapeConfig
    {
        public FileTarget Target { get; set; }
        public LoggingRule Rule { get; set; }
    }
}