using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using AngleSharp.Xml;

namespace DiaryScraperCore
{
    public static class AngleExtensions
    {
        public static string GetHtml(this IHtmlDocument doc)
        {
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms))
                using (var sr = new StreamReader(ms))
                {
                    doc.ToHtml(sw, XmlMarkupFormatter.Instance);
                    sw.Flush();
                    ms.Position = 0;
                    return sr.ReadToEnd();
                }
            }
        }

        public static void WriteToFile(this IHtmlDocument doc, string filePath, Encoding encoding)
        {
            using (var sw = new StreamWriter(File.Open(filePath, FileMode.Create), encoding))
            {
                doc.ToHtml(sw, XmlMarkupFormatter.Instance);
            }
        }

        public static async Task<IHtmlDocument> FromFileAsync(this HtmlParser parser, string filePath, Encoding encoding, CancellationToken cancellationToken)
        {
            using (var sr = new StreamReader(File.Open(filePath, FileMode.Open), encoding))
            {
                return await parser.ParseAsync(sr.BaseStream, cancellationToken);
            }
        }
    }
}