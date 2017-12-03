using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Parser.Html;
using Microsoft.Extensions.Logging;

namespace DiaryScraperCore
{
    public class DiaryArchiver : DiaryAsyncImplementationBase
    {
        private readonly ILogger<DiaryArchiver> _logger;
        public ArchiveTaskProgress Progress = new ArchiveTaskProgress();
        private readonly ArchiveTaskDescriptor _descriptor;
        private readonly HtmlParser _parser;
        private string DiaryDir => _descriptor.WorkingDir;
        private string ArchiveDir => Path.Combine(DiaryDir, Constants.ArchiveDir);
        private string PostsDir => Path.Combine(DiaryDir, Constants.PostsDir);
        private string ImagesDir => Path.Combine(DiaryDir, Constants.ImagesDir);
        private string ArchivePostsDir => Path.Combine(ArchiveDir, Constants.PostsDir);
        private string ArchiveImagesDir => Path.Combine(ArchiveDir, Constants.ImagesDir);
        public DiaryArchiver(ILogger<DiaryArchiver> logger, ArchiveTaskDescriptor descriptor)
        {
            _logger = logger;
            _descriptor = descriptor;
            var config = new Configuration().WithCss();
            _parser = new HtmlParser(config);
        }

        protected override ILogger Logger => _logger;

        public override void DoWork(CancellationToken cancellationToken)
        {
            DoWorkAsync(cancellationToken).Wait();
        }

        public async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(ArchivePostsDir);
            Directory.CreateDirectory(ArchiveImagesDir);

            CopyDir(PostsDir, ArchivePostsDir);
            cancellationToken.ThrowIfCancellationRequested();
            CopyDir(ImagesDir, ArchiveImagesDir);

            var templatePath = Path.Combine(_descriptor.WorkingDir, Constants.AccountPagesDir, AccountPagesFileNames.LastPosts);
            var indexPath = Path.Combine(ArchiveDir, "index.htm");
            File.Copy(templatePath, indexPath);

            var indexDoc = await _parser.FromFileAsync(indexPath, Encoding.GetEncoding(1251), cancellationToken);
            foreach (var postDiv in indexDoc.QuerySelectorAll("div.singlePost"))
            {
                postDiv.Remove();
            }

            var barDiv = indexDoc.QuerySelector("div#pageBar");

            var postFiles = Directory.GetFiles(ArchivePostsDir);
            foreach (var postFile in postFiles)
            {
                using (var postDoc = await _parser.FromFileAsync(postFile, Encoding.GetEncoding(1251), cancellationToken))
                {
                    var postDiv = postDoc.QuerySelector("div.singlePost");
                    if (postDiv == null)
                    {
                        continue;
                    }

                    postDiv.ClassList.Remove("countSecond");
                    postDiv.ClassList.Remove("countFirst");
                    postDiv.ClassList.Remove("lastPost");

                    var newIndexDiv = indexDoc.CreateElement("div");
                    barDiv.Before(newIndexDiv);
                    newIndexDiv.OuterHtml = postDiv.OuterHtml;
                }
            }

            indexDoc.WriteToFile(indexPath, Encoding.GetEncoding(1251));
        }

        private void CopyDir(string sourceDir, string destDir)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*",
                SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourceDir, "*.*",
                SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourceDir, destDir), true);
            }
        }

        public override void SetError(string error)
        {
            Progress.Error = error;
        }
    }
}