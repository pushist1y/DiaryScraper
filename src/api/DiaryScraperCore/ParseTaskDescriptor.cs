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

        public DiaryParserProgress Progress { get; set; }

        public override string Error => Progress?.Error ?? _error;

    }

    

    public class DiaryParserProgress
    {
        public int PostsDiscovered { get; set; }
        public int PostsProcessed { get; set; }
        public string CurrentPost { get; set; }
        public string Error { get; set; }
    }

    

    
}