using System;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
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
}