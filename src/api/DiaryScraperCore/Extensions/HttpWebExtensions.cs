using System;
using System.Collections.Generic;
using System.Linq;
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

        public static byte[] Combine(this IEnumerable<byte[]> listOfByteArrays)
        {
            byte[] ret = new byte[listOfByteArrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in listOfByteArrays)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }

        public static byte[] GetDiaryLoginPostData(string login, string pass, string signature)
        {
            var enc1251 = Encoding.GetEncoding(1251);
            var bytesBuilder = new List<byte[]>();
            bytesBuilder.Add(Encoding.UTF8.GetBytes("user_login="));

            var buf = Encoding.Convert(Encoding.UTF8, enc1251, Encoding.UTF8.GetBytes(login));
            bytesBuilder.Add(WebUtility.UrlEncodeToBytes(buf, 0, buf.Length));
            bytesBuilder.Add(Encoding.UTF8.GetBytes("&user_pass="));
            var buf2 = Encoding.Convert(Encoding.UTF8, enc1251, Encoding.UTF8.GetBytes(pass));

            bytesBuilder.Add(WebUtility.UrlEncodeToBytes(buf2, 0, buf2.Length));
            bytesBuilder.Add(Encoding.UTF8.GetBytes("&signature=" + signature));

            var res = bytesBuilder.Combine().ToArray();
            return res;
        }
    }
}