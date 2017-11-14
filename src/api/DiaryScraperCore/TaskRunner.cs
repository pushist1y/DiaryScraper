using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public class TaskRunner
    {
        // private static TaskRunner _instance;

        // public static TaskRunner Instance
        // {
        //     get
        //     {
        //         if (_instance == null)
        //         {
        //             _instance = new TaskRunner();
        //         }
        //         return _instance;
        //     }
        // }

        private ILogger<DiaryScraper> _logger;
        public TaskRunner(ILogger<DiaryScraper> logger)
        {
            _logger = logger;
        }

        private ReadOnlyCollection<ScrapeTaskDescriptor> _readOnlyTasks;
        public ReadOnlyCollection<ScrapeTaskDescriptor> TasksView
            => _readOnlyTasks ?? (_readOnlyTasks = new ReadOnlyCollection<ScrapeTaskDescriptor>(_tasks));
        private List<ScrapeTaskDescriptor> _tasks = new List<ScrapeTaskDescriptor>();
        public void AddTask(ScrapeTaskDescriptor newTask, string login = null, string password = null)
        {
            _tasks.Add(newTask);
            
            if (!Directory.Exists(newTask.WorkingDir))
            {
                try
                {
                    Directory.CreateDirectory(newTask.WorkingDir);
                }
                catch (Exception e)
                {
                    newTask.Error = e.Message;
                    newTask.InnerTask = Task.FromException(e);
                }
            }

            var worker = new DiaryScraper(newTask, login, password, _logger);
            worker.Run();
        }

        public ScrapeTaskDescriptor RemoveTask(string guidString)
        {
            var task = _tasks.FirstOrDefault(t => t.GuidString == guidString);
            
            if(task==null)
            {
                return null;
            }

            task.TokenSource.Cancel();
            task.InnerTask.Wait();
            return task;
        }
    }
}