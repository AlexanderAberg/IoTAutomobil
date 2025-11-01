using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace IoTAutomobil.DTC
{
    internal static class DtcInfoUtil
    {
        public static string BuildInfoUrl(string code)
        {
            return $"https://club.autodoc.se/obd-codes/{code.ToLowerInvariant()}";
        }

        public static string StripTags(string html)
        {
            var text = Regex.Replace(html, "<.*?>", " ", RegexOptions.Singleline);
            text = WebUtility.HtmlDecode(text);
            return Regex.Replace(text, @"\\s+", " ").Trim();
        }

        public static HttpClient CreateHttp(string? referer = null)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };
            var http = new HttpClient(handler);
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
            http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("sv-SE,sv;q=0.9,en-US;q=0.8,en;q=0.7");
            if (!string.IsNullOrWhiteSpace(referer))
            {
                try { http.DefaultRequestHeaders.Referrer = new Uri(referer); } catch { }
            }
            return http;
        }

        public static string MakeAbsoluteUrl(string baseUrl, string href)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(href)) return baseUrl;
                var baseUri = new Uri(baseUrl, UriKind.Absolute);
                if (Uri.TryCreate(href, UriKind.Absolute, out var abs)) return abs.ToString();
                if (Uri.TryCreate(baseUri, href, out var rel)) return rel.ToString();
            }
            catch { }
            return baseUrl;
        }
    }
}