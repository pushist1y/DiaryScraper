using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public class DiaryParserFactory : WorkerFactoryBase
    {
        public DiaryParserFactory(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public DiaryParser GetParser(ParseTaskDescriptor descriptor)
        {
            var options = new DiaryParserOptions();
            options.DiaryDir = descriptor.WorkingDir;
            var cfg = ConfigureLog(descriptor.WorkingDir);
            var logger = _serviceProvider.GetRequiredService<ILogger<DiaryParser>>();

            descriptor.Parser = new DiaryParser(options, logger);
            descriptor.Parser.ParseFinished += (s,e) => {
                UnsetLog(cfg);
            };

            return descriptor.Parser;
        }
    }
}