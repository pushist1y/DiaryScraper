using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DiaryScraperCore
{

    public class ParseTaskRunner: TaskRunnerBase<ParseTaskDescriptor>
    {
        private readonly DiaryParserFactory _dpFac;
        public ParseTaskRunner(DiaryParserFactory dpFac)
        {
            _dpFac = dpFac;
        }

        public void AddTask(ParseTaskDescriptor newTask)
        {
            Tasks.Add(newTask);
            var parser =  _dpFac.GetParser(newTask);
            parser?.Run();
        }

    }
}