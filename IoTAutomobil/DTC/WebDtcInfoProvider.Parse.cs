using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace IoTAutomobil.DTC
{
    internal sealed partial class WebDtcInfoProvider
    {
        private static string? TryDiscoverPerCodeUrl(string listHtml, string code)
        {
            if (string.IsNullOrWhiteSpace(listHtml)) return null;

            try
            {
                var aRx = new Regex(@"<a\s+(?:[^>]*?)href\s*=\s*(?:""(?<href>[^""]+)""|'(?<href>[^']+)')[^>]*>(?<text>.*?)</a>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match m in aRx.Matches(listHtml))
                {
                    var text = DtcInfoUtil.StripTags(m.Groups["text"].Value);
                    if (text.IndexOf(code, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return DtcInfoUtil.MakeAbsoluteUrl("https://club.autodoc.se/obd-codes/all", m.Groups["href"].Value);
                    }
                }

                var hrefRx = new Regex($@"<a\s+[^>]*href\s*=\s*(?:""(?<href>[^""]+)""|'(?<href>[^']+)')[^>]*>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match m in hrefRx.Matches(listHtml))
                {
                    var href = m.Groups["href"].Value;
                    if (href.IndexOf(code, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return DtcInfoUtil.MakeAbsoluteUrl("https://club.autodoc.se/obd-codes/all", href);
                    }
                }
            }
            catch { }

            return null;
        }

        private static string? TryExtractFromCodePage(string html, string code)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            try
            {
                var og = Regex.Match(html, @"<meta\s+property\s*=\s*[""']og:description[""']\s+content\s*=\s*[""'](?<c>[^""']+)[""']",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (og.Success)
                {
                    var text = DtcInfoUtil.StripTags(og.Groups["c"].Value);
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }

                var meta = Regex.Match(html, @"<meta\s+name\s*=\s*[""']description[""']\s+content\s*=\s*[""'](?<c>[^""']+)[""']",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (meta.Success)
                {
                    var text = DtcInfoUtil.StripTags(meta.Groups["c"].Value);
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }

                var ld = Regex.Match(html, @"<script[^>]*type\s*=\s*[""']application/ld\+json[""'][^>]*>(?<json>.*?)</script>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (ld.Success)
                {
                    var json = WebUtility.HtmlDecode(ld.Groups["json"].Value);
                    var desc = Regex.Match(json, @"""description""\s*:\s*""(?<d>[^""]+)""", RegexOptions.IgnoreCase);
                    if (desc.Success)
                    {
                        var text = WebUtility.HtmlDecode(desc.Groups["d"].Value);
                        text = Regex.Replace(text, @"\s+", " ").Trim();
                        if (!string.IsNullOrWhiteSpace(text)) return text;
                    }
                }

                var h1 = Regex.Match(html, @"<h1[^>]*>(?<c>.*?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (h1.Success)
                {
                    var text = DtcInfoUtil.StripTags(h1.Groups["c"].Value);
                    if (!string.IsNullOrWhiteSpace(text) && !text.Equals(code, StringComparison.OrdinalIgnoreCase))
                        return text;
                }

                var idx = html.IndexOf(code, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var tail = html.Substring(idx, Math.Min(1500, html.Length - idx));
                    var text = DtcInfoUtil.StripTags(tail);
                    var cleaned = Regex.Replace(text, $@"^{Regex.Escape(code)}\s*[:\-–]\s*", "", RegexOptions.IgnoreCase);
                    var firstSentence = Regex.Split(cleaned, @"(?<=[\.!?])\s+").FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(firstSentence))
                        return firstSentence.Length > 240 ? firstSentence[..240] + "…" : firstSentence;
                }
            }
            catch { }

            return null;
        }

        private static string? TryExtractFromList(string html, string code)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            try
            {
                var rowRx = new Regex($@"<tr[^>]*>.*?\b{Regex.Escape(code)}\b.*?</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var m = rowRx.Match(html);
                if (m.Success)
                {
                    var rowHtml = m.Value;
                    var cellRx = new Regex(@"<t[dh][^>]*>(?<cell>.*?)</t[dh]>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    var cells = cellRx.Matches(rowHtml).Select(mm => DtcInfoUtil.StripTags(mm.Groups["cell"].Value)).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    if (cells.Count >= 2)
                    {
                        var desc = cells[1];
                        if (!string.IsNullOrWhiteSpace(desc)) return desc;
                    }
                }

                var liRx = new Regex($@"<li[^>]*>[^<]*\b{Regex.Escape(code)}\b[^<]*:(?<desc>.*?)</li>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var li = liRx.Match(html);
                if (li.Success)
                {
                    var desc = DtcInfoUtil.StripTags(li.Groups["desc"].Value);
                    if (!string.IsNullOrWhiteSpace(desc)) return desc;
                }
            }
            catch { }

            return null;
        }
    }
}