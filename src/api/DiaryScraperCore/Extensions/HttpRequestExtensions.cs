using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace DiaryScraperCore
{
    public static class HttpRequestExtensions
    {
        public static string GetQueryParameter(this HttpRequest request, string key)
        {
            if (!request.Query.TryGetValue(key, out var vals))
            {
                return null;
            }

            return vals.FirstOrDefault();
        }

        public static T? GetQueryParameter<T>(this HttpRequest request, string key) where T: struct
        {
            if (!request.Query.TryGetValue(key, out var vals))
            {
                return null;
            }

            var strVal = vals.FirstOrDefault();
            if (string.IsNullOrEmpty(strVal))
            {
                return null;
            }

            var result = (T)Convert.ChangeType(strVal, typeof(T));
            return result;
        }
    }
}