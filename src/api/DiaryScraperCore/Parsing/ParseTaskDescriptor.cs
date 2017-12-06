using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DiaryScraperCore
{
    public class ParseTaskDescriptor : TaskDescriptorBase
    {
        public override Task InnerTask => Parser?.Worker;

        public override CancellationTokenSource TokenSource => Parser?.TokenSource;

        [JsonIgnore]
        public DiaryParser Parser { get; set; }

        public ParseTaskProgress Progress => Parser?.Progress;

        public override string Error => Progress?.Error ?? _error;

    }

}