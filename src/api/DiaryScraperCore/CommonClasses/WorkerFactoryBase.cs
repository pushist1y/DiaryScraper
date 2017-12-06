using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
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

        protected ScrapeContext GetContext(string dbPath)
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite($@"Data Source={dbPath}");
            optionsBuilder.EnableSensitiveDataLogging();
            var context = new ScrapeContext(optionsBuilder.Options);
            context.Database.Migrate();

            return context;
        }
    }

    public class NLogScrapeConfig
    {
        public FileTarget Target { get; set; }
        public LoggingRule Rule { get; set; }
    }
}