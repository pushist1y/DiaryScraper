using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public class DiaryArchiverFactory : WorkerFactoryBase
    {
        public DiaryArchiverFactory(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public DiaryArchiver GetArchiver(ArchiveTaskDescriptor descriptor)
        {
            var cfg = ConfigureLog(descriptor.WorkingDir);
            var logger = _serviceProvider.GetRequiredService<ILogger<DiaryArchiver>>();
            try
            {
                EnsureDirs(descriptor);
                descriptor.Archiver = new DiaryArchiver(logger, descriptor);
                descriptor.Archiver.WorkFinished += (s, e) =>
                {
                    UnsetLog(cfg);
                };
                return descriptor.Archiver;
            }
            catch (Exception e)
            {
                UnsetLog(cfg);
                descriptor.SetError(e.Message);
                return null;
            }
        }

        private void EnsureDirs(ArchiveTaskDescriptor descriptor)
        {
            var scrapeDbPath = Path.Combine(descriptor.WorkingDir, Constants.DbName);
            if (!File.Exists(scrapeDbPath))
            {
                throw new FileNotFoundException("Не найден файл с БД скачивания", scrapeDbPath);
            }

            var templatePath = Path.Combine(descriptor.WorkingDir, Constants.AccountPagesDir, AccountPagesFileNames.LastPosts);
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException("Не найден файл с шаблоном для архивирования", templatePath);
            }

            var archivePath = Path.Combine(descriptor.WorkingDir, Constants.ArchiveDir);
            if (Directory.Exists(archivePath))
            {
                Directory.Delete(archivePath, true);
            }
            Directory.CreateDirectory(archivePath);
        }
    }
}