using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public abstract class TaskRunnerBase<T> where T: TaskDescriptorBase
    {
        protected ReadOnlyCollection<T> ReadOnlyTasks;
        public ReadOnlyCollection<T> TasksView
            => ReadOnlyTasks ?? (ReadOnlyTasks = new ReadOnlyCollection<T>(Tasks));
        protected List<T> Tasks = new List<T>();

        public T RemoveTask(string guidString)
        {
            var task = Tasks.FirstOrDefault(t => t.GuidString == guidString);

            if (task == null)
            {
                return null;
            }

            task.TokenSource.Cancel();
            task.InnerTask.Wait();
            return task;
        }
    }
}