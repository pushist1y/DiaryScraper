using System;

namespace DiaryScraperCore
{
    public class ScrapeTaskProgress : TaskProgress
    {
        public override int Percent
        {
            get
            {
                var disc = GetValue<int>(ScrapeProgressNames.DatePagesDiscovered);
                var proc = GetValue<int>(ScrapeProgressNames.DatePagesProcessed);
                if (disc == 0)
                {
                    return RangeDiscovered ? 100 : 0;
                }
                return Convert.ToInt32(100.0 * proc / disc);
            }
        }

        public ScrapeTaskProgress()
        {
            Values[ScrapeProgressNames.CurrentUrl] = "";
            Values[ScrapeProgressNames.BytesDownloaded] = 0;
            Values[ScrapeProgressNames.PagesDownloaded] = 0;
            Values[ScrapeProgressNames.ImagesDownloaded] = 0;
            Values[ScrapeProgressNames.DatePagesDiscovered] = 0;
            Values[ScrapeProgressNames.DatePagesProcessed] = 0;
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