using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiaryScraperCore
{
    public abstract class TaskDescriptorBase
    {
        [JsonIgnore]
        protected string _error = null;
        public string WorkingDir { get; set; }
        [JsonIgnore]
        public Guid Guid { get; set; } = Guid.NewGuid();
        public string GuidString => this.Guid.ToString("n");
        public virtual string Error => _error;
        [JsonIgnore]
        public abstract Task InnerTask { get; }
        public TaskStatus? Status => InnerTask?.Status;
        [JsonIgnore]
        public abstract CancellationTokenSource TokenSource { get; }
        public void SetError(string error)
        {
            _error = error;
        }

    }
}