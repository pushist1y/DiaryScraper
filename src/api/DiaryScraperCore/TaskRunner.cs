using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public class ParseTaskRunner
    {
        private readonly DiaryParserFactory _dpFac;
        public ParseTaskRunner(DiaryParserFactory dpFac)
        {
            _dpFac = dpFac;
        }

        private List<ParseTaskDescriptor> _tasks = new List<ParseTaskDescriptor>();

        private ReadOnlyCollection<ParseTaskDescriptor> _readOnlyTasks;
        public ReadOnlyCollection<ParseTaskDescriptor> TasksView
            => _readOnlyTasks ?? (_readOnlyTasks = new ReadOnlyCollection<ParseTaskDescriptor>(_tasks));

        public void AddTask(ParseTaskDescriptor newTask)
        {
            _tasks.Add(newTask);
            var parser =  _dpFac.GetParser(newTask);
            parser?.Run();
        }


    }
    public class TaskRunner
    {
        private DiaryScraperFactory _dsFac;
        public TaskRunner(DiaryScraperFactory dsFac)
        {
            _dsFac = dsFac;
        }

        private ReadOnlyCollection<ScrapeTaskDescriptor> _readOnlyTasks;
        public ReadOnlyCollection<ScrapeTaskDescriptor> TasksView
            => _readOnlyTasks ?? (_readOnlyTasks = new ReadOnlyCollection<ScrapeTaskDescriptor>(_tasks));
        private List<ScrapeTaskDescriptor> _tasks = new List<ScrapeTaskDescriptor>();
        public void AddTask(ScrapeTaskDescriptor newTask, string login = null, string password = null)
        {
            _tasks.Add(newTask);
            var scraper = _dsFac.GetScraper(newTask, login, password);
            scraper?.Run();
        }

        public ScrapeTaskDescriptor RemoveTask(string guidString)
        {
            var task = _tasks.FirstOrDefault(t => t.GuidString == guidString);

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