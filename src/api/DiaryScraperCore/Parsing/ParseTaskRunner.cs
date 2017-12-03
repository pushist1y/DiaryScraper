using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

        public ParseTaskDescriptor RemoveTask(string guidString)
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