namespace DiaryScraperCore
{
    public class ScrapeTaskRunner: TaskRunnerBase<ScrapeTaskDescriptor>
    {
        private DiaryScraperFactory _dsFac;
        public ScrapeTaskRunner(DiaryScraperFactory dsFac)
        {
            _dsFac = dsFac;
        }

        public void AddTask(ScrapeTaskDescriptor newTask, string login = null, string password = null)
        {
            Tasks.Add(newTask);
            var scraper = _dsFac.GetScraper(newTask, login, password);
            scraper?.Run();
        }

        
    }
}