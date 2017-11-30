namespace DiaryScraperCore
{
    public static class Constants
    {
        public const string ImagesDir = "images";
        public const string PostsDir = "posts";
        public const string PostEditsDir = "postedits";
        public const string DbName = "scrape.db";
        public const int DownloadRetryCount = 3;
    }

    public static class ScrapeProgressNames
    {
        public const string CurrentUrl = "Текущий URL";
        public const string PagesDownloaded = "Страниц скачано";
        public const string ImagesDownloaded = "Изображений скачано";
        public const string BytesDownloaded = "Байт скачано";
        public const string DatePagesDiscovered = "Дат обнаружено";
        public const string DatePagesProcessed = "Дат обработано";

    }

    public static class ParseProgressNames
    {
        public const string PostsDiscovered = "Постов обнаружено";
        public const string PostsProcessed = "Постов обработано";
        public const string CurrentFile = "Текущий файл";
    }
}