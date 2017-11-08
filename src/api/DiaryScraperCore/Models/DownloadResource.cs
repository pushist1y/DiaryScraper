using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiaryScraperCore
{
    public class DownloadResource
    {
        [Key]
        public int Id { get; set; }
        public string Url { get; set; }
        public string LocalPath { get; set; }
        [NotMapped]
        public bool JustCreated { get; set; } = false;
    }

    public class DiaryPost : DownloadResource
    {

    }

    public class DiaryImage : DownloadResource
    {

    }
}