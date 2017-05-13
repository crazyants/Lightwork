using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Lightwork.Core.Utilities;

namespace Lightwork.ServiceApi.Client
{
    public static class Extensions
    {
        public static StringContent AsJsonContent(this object source)
        {
            return new StringContent(
                JsonHelper.Serialize(source),
                Encoding.UTF8,
                "application/json");
        }

        public static string ToApiUrl(this string source, string baseAddress, params KeyValuePair<string, object>[] queryStringParameters)
        {
            var sb = new StringBuilder();
            sb.Append(baseAddress);

            if (!baseAddress.EndsWith("/"))
            {
                sb.Append("/");
            }

            sb.Append(source);

            return ToUrl(sb.ToString(), queryStringParameters);
        }

        public static string ToUrl(this string url, params KeyValuePair<string, object>[] queryStringParameters)
        {
            if (queryStringParameters == null)
            {
                return url;
            }

            var sb = new StringBuilder();
            sb.Append(url).Append("?");

            var paramIndex = 0;
            foreach (var qsParam in queryStringParameters)
            {
                if (paramIndex++ >= 1)
                {
                    sb.Append("&");
                }

                sb.Append(qsParam.Key);
                sb.Append("=");
                if (qsParam.Value != null)
                {
                    sb.Append(WebUtility.UrlEncode(qsParam.Value.ToString()));
                }
            }

            return sb.ToString();
        }
    }
}
