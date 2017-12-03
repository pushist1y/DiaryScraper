using System;
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

        public DiaryParserProgress Progress => Parser?.Progress;

        public override string Error => Progress?.Error ?? _error;

    }



    public class DiaryParserProgress : TaskProgress
    {
        public override int Percent
        {
            get
            {
                var disc = GetValue<int>(ParseProgressNames.PostsDiscovered);
                var proc = GetValue<int>(ParseProgressNames.PostsProcessed);
                if (disc == 0)
                {
                    return RangeDiscovered ? 100 : 0;
                }
                return Convert.ToInt32(100.0 * proc / disc);
            }
        }

        public DiaryParserProgress()
        {
            Values[ParseProgressNames.CurrentFile] = "";
            Values[ParseProgressNames.PostsDiscovered] = 0;
            Values[ParseProgressNames.PostsProcessed] = 0;
        }
    }




}