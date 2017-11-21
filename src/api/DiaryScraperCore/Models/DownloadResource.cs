using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text.RegularExpressions;

namespace DiaryScraperCore
{
    public class DownloadResource
    {
        [Key]
        public int Id { get; set; }
        public string Url { get; set; }
        public string LocalPath { get; set; }
        [NotMapped]
        public byte[] Data { get; set; }
        [NotMapped]
        public bool JustCreated { get; set; } = false;

        [NotMapped]
        public string RelativePath
        {
            get
            {
                var match = Regex.Match(LocalPath, @"[\\\/]?([^\\\/]+[\\\/][^\\\/]*)$");
                if (!match.Success)
                {
                    return LocalPath;
                }
                return match.Groups[1].Value;
            }
        }

        public string GenerateLocalPath(string prefix)
        {
            var fName = "";
            var fNameMatch = Regex.Match(Url, @"([^\/]*)$");

            if (fNameMatch.Success)
            {
                fName = prefix + fNameMatch.Groups[1].Value;
            }
            else
            {
                fName = prefix + Guid.NewGuid().ToString("n") + ".dat";
            }
            this.LocalPath = Path.Combine(DirName, fName);
            return this.LocalPath;
        }

        [NotMapped]
        public virtual string DirName => "data";
    }

    public class DiaryPost : DownloadResource
    {
        [NotMapped]
        public override string DirName => Constants.PostsDir;
    }

    public class DiaryImage : DownloadResource
    {
        [NotMapped]
        public override string DirName => Constants.ImagesDir;
    }
}