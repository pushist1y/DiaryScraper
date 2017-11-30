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
        public bool JustCreated { get; set; } = false;

        [NotMapped]
        public string RelativePath
        {
            get
            {
                if (LocalPath == null)
                {
                    return null;
                }
                var match = Regex.Match(LocalPath, @"[\\\/]?([^\\\/]+[\\\/][^\\\/]*)$", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    return LocalPath;
                }
                return match.Groups[1].Value;
            }
        }

        public virtual string GenerateLocalPath(string prefix)
        {
            var fName = "";
            var fNameMatch = Regex.Match(Url, @"([^\/]*)$", RegexOptions.IgnoreCase);

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
        public DiaryPostEdit PostEdit { get; set; }
    }

    public class DiaryPostEdit : DownloadResource
    {
        public int PostId { get; set; }
        [ForeignKey("PostId")]
        [Required]
        public DiaryPost Post { get; set; }
        [NotMapped]
        public override string DirName => Constants.PostEditsDir;
        public override string GenerateLocalPath(string prefix)
        {
            var fName = "";
            if (Post == null)
            {
                fName = prefix + Guid.NewGuid().ToString("n") + ".htm";
            }
            else
            {
                fName = Regex.Replace(Path.GetFileName(Post.RelativePath), @"\.htm$", "_edit.htm");
            }
            this.LocalPath = Path.Combine(DirName, fName);
            return this.LocalPath;
        }
    }

    public class DiaryImage : DownloadResource
    {
        [NotMapped]
        public override string DirName => Constants.ImagesDir;
    }

    public class DiaryDatePage : DownloadResource
    {
        public DateTime PostDate { get; set; }
    }
}