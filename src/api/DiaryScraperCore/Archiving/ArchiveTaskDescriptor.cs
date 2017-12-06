using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DiaryScraperCore
{
    public class ArchiveTaskDescriptor : TaskDescriptorBase
    {
        public override Task InnerTask => Archiver?.Worker;

        public override CancellationTokenSource TokenSource => Archiver?.TokenSource;

        [JsonIgnore]
        public DiaryArchiver Archiver { get; set; }

        public ParseTaskProgress Progress => Archiver?.Progress;

        public override string Error => Progress?.Error ?? _error;

    }

}