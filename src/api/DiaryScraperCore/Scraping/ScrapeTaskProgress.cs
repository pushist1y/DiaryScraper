using System;

namespace DiaryScraperCore
{
    public class ScrapeTaskProgress : TaskProgress
    {
        public ScrapeTaskProgress(): base(ScrapeProgressNames.DatePagesProcessed, ScrapeProgressNames.DatePagesDiscovered)
        {
            Values[ScrapeProgressNames.CurrentUrl] = "";
            Values[ScrapeProgressNames.BytesDownloaded] = 0;
            Values[ScrapeProgressNames.PagesDownloaded] = 0;
            Values[ScrapeProgressNames.ImagesDownloaded] = 0;
        }

        public void PageDownloaded(byte[] data)
        {
            IncrementInt(ScrapeProgressNames.BytesDownloaded, data.Length);
            IncrementInt(ScrapeProgressNames.PagesDownloaded, 1);
        }

        public void PageDownloaded(string html)
        {
            IncrementInt(ScrapeProgressNames.BytesDownloaded, System.Text.Encoding.ASCII.GetByteCount(html));
            IncrementInt(ScrapeProgressNames.PagesDownloaded, 1);
        }

        public void ImageDownloaded(byte[] data)
        {
            IncrementInt(ScrapeProgressNames.BytesDownloaded, data.Length);
            IncrementInt(ScrapeProgressNames.ImagesDownloaded, 1);
        }
    }
}