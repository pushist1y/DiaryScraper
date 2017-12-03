using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DiaryScraperCore
{

    public class ArchiveTaskRunner : TaskRunnerBase<ArchiveTaskDescriptor>
    {
        private readonly DiaryArchiverFactory _daFac;
        public ArchiveTaskRunner(DiaryArchiverFactory daFac)
        {
            _daFac = daFac;
        }

        public void AddTask(ArchiveTaskDescriptor newTask)
        {
            Tasks.Add(newTask);
            var parser = _daFac.GetArchiver(newTask);
            parser?.Run();
        }

    }
}