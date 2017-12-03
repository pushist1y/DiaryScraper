using System.Collections.Generic;

namespace DiaryScraperCore
{
    public static class Constants
    {
        public const string ImagesDir = "images";
        public const string PostsDir = "posts";
        public const string PostEditsDir = "postedits";
        public const string AccountPagesDir = "accountpages";
        public const string DbName = "scrape.db";
        public const int DownloadRetryCount = 3;

        public static readonly List<string> SettingsUrls = new List<string>();
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

    public static class AccountPagesFileNames
    {
        public const string DiaryMain = "diary.htm";
        public const string MemberAccess = "member_access.htm";
        public const string DiaryAccess = "diary_access.htm";
        public const string DiaryCommentAccess = "diary_commentaccess.htm";
        public const string DiaryPch = "diary_pch.htm";
        public const string Member = "member.htm";
        public const string Tags = "tags.htm";
        public const string Profile = "profile.htm";
        public const string Geography = "geography.htm";
        public const string Owner = "owner.htm";
        public const string LastPosts = "lastposts.htm";
    }
}