using System;
using System.Net;
using System.Text;

namespace DiaryScraperCore
{
    public static class HttpWebExtensions
    {
        public static HttpWebResponse GetResponseNoException(this HttpWebRequest req)
        {
            try
            {
                return (HttpWebResponse)req.GetResponse();
            }
            catch (WebException we)
            {
                var resp = we.Response as HttpWebResponse;
                if (resp == null)
                    throw;
                return resp;
            }
        }

        public static string DownloadAnsiString(this WebClient client, string url)
        {
            var bytes = client.DownloadData(url);
            return bytes.AsAnsiString();
        }

        public static string DownloadAnsiString(this WebClient client, Uri uri)
        {
            var bytes = client.DownloadData(uri);
            return bytes.AsAnsiString();
        }

        public static string AsAnsiString(this byte[] bytes)
        {
            var enc1251 = Encoding.GetEncoding(1251);
            var html = enc1251.GetString(bytes);
            return html;
        }
    }
}