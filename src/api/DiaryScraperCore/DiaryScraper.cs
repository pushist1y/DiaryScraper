using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiaryScraperCore
{
    public class DiaryScraper
    {
        private string _login;
        private string _pass;
        private ScrapeTaskDescriptor _descriptor;
        public DiaryScraper(ScrapeTaskDescriptor descriptor) : this(descriptor, null, null)
        {

        }

        public DiaryScraper(ScrapeTaskDescriptor descriptor, string login, string pass)
        {
            _login = login;
            _pass = pass;
            _descriptor = descriptor;
        }

        public Task Run()
        {
            var src = new CancellationTokenSource();
            _descriptor.Token = src.Token;
            _descriptor.InnerTask = new Task(() => DoWork(_descriptor.Token));
            _descriptor.InnerTask.Start();
            return _descriptor.InnerTask;
        }

        private void DoWork(CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            while (true)
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    _descriptor.Error = "Process was cancelled by user";
                    return;
                }
                if(DateTime.Now - startTime > TimeSpan.FromMinutes(5))
                {
                    return;
                }

                Thread.Sleep(1000);
                _descriptor.Progress.LastUpdated = DateTime.Now;
                _descriptor.Progress.BytesDownloaded += 1024;
            }
        }
    }
}