using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace IoTAutomobil
{
    internal interface IDtcProvider
    {
        bool TryGetRandom(out string code);
    }

    internal sealed class CsvDtcProvider : IDtcProvider
    {
        private readonly string[] _codes;
        private readonly Random _random;

        public CsvDtcProvider(string filePath, Random random)
        {
            _random = random;
            _codes = Load(filePath);
        }

        private static string[] Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return Array.Empty<string>();

                var codeRx = new Regex(@"^[PBCU]\d{4}$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

                return File.ReadLines(filePath)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"))
                    .Select(line =>
                    {
                        var match = Regex.Match(line, @"\b([PBCU]\d{4})\b", RegexOptions.IgnoreCase);
                        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
                    })
                    .Where(code => code is not null)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Cast<string>()
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public bool TryGetRandom(out string code)
        {
            if (_codes.Length == 0)
            {
                code = string.Empty;
                return false;
            }
            code = _codes[_random.Next(_codes.Length)];
            return true;
        }
    }

    internal sealed class FallbackDtcProvider : IDtcProvider
    {
        private readonly string[] _codes = { "P0300", "P0420", "P0171", "P0455", "P0133" };
        private readonly Random _random;

        public FallbackDtcProvider(Random random) => _random = random;

        public bool TryGetRandom(out string code)
        {
            code = _codes[_random.Next(_codes.Length)];
            return true;
        }
    }

    internal sealed class DtcInfo
    {
        public string Code { get; }
        public string? Title { get; }
        public string Url { get; }

        public DtcInfo(string code, string? title, string url)
        {
            Code = code;
            Title = title;
            Url = url;
        }
    }

    internal interface IDtcInfoProvider
    {
        bool TryGetInfo(string code, out DtcInfo info);
    }

    internal sealed class ChainDtcInfoProvider : IDtcInfoProvider
    {
        private readonly IDtcInfoProvider[] _providers;
        private readonly bool _debug =
            string.Equals(Environment.GetEnvironmentVariable("DTC_INFO_DEBUG"), "1", StringComparison.OrdinalIgnoreCase);

        public ChainDtcInfoProvider(params IDtcInfoProvider[] providers) => _providers = providers;

        public bool TryGetInfo(string code, out DtcInfo info)
        {
            foreach (var p in _providers)
            {
                if (p.TryGetInfo(code, out info))
                {
                    if (_debug)
                        Console.WriteLine($"[DTC] Info from {p.GetType().Name} for {code}: {info.Title ?? "(no title)"}");
                    return true;
                }
            }
            info = new DtcInfo(code, null, DtcInfoUtil.BuildInfoUrl(code));
            if (_debug)
                Console.WriteLine($"[DTC] Info not found in providers for {code}. Using fallback URL only.");
            return false;
        }
    }

    internal static class DtcInfoUtil
    {
        public static string BuildInfoUrl(string code)
        {
            // Direct per-code page for human viewing
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
            // Accept-Encoding is set by AutomaticDecompression
            if (!string.IsNullOrWhiteSpace(referer))
            {
                try { http.DefaultRequestHeaders.Referrer = new Uri(referer); } catch { /* ignore */ }
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

    internal sealed class CsvDtcInfoProvider : IDtcInfoProvider
    {
        private readonly Dictionary<string, string> _map;

        public CsvDtcInfoProvider(string filePath)
        {
            _map = LoadDescriptions(filePath);
        }

        private static Dictionary<string, string> LoadDescriptions(string filePath)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(filePath)) return dict;

                var rx = new Regex(@"^\s*(?<code>[PBCU]\d{4})\s*(?:[,;\t]\s*(?<desc>.+))?$", RegexOptions.IgnoreCase);

                foreach (var raw in File.ReadLines(filePath))
                {
                    var line = raw.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    var m = rx.Match(line);
                    if (!m.Success) continue;

                    var code = m.Groups["code"].Value.ToUpperInvariant();
                    var desc = m.Groups["desc"].Value.Trim();

                    if (!string.IsNullOrWhiteSpace(desc))
                        dict[code] = desc;
                }
            }
            catch { }
            return dict;
        }

        public bool TryGetInfo(string code, out DtcInfo info)
        {
            var up = code.ToUpperInvariant();
            if (_map.TryGetValue(up, out var desc))
            {
                info = new DtcInfo(up, desc, DtcInfoUtil.BuildInfoUrl(up));
                return true;
            }
            info = new DtcInfo(up, null, DtcInfoUtil.BuildInfoUrl(up));
            return false;
        }
    }

    internal sealed class HeuristicDtcInfoProvider : IDtcInfoProvider
    {
        public bool TryGetInfo(string code, out DtcInfo info)
        {
            var up = code.ToUpperInvariant();
            if (!Regex.IsMatch(up, @"^[PBCU]\d{4}$"))
            {
                info = new DtcInfo(up, null, DtcInfoUtil.BuildInfoUrl(up));
                return false;
            }

            var system = up[0] switch
            {
                'P' => "Powertrain",
                'B' => "Body",
                'C' => "Chassis",
                'U' => "Network",
                _ => "Unknown"
            };

            var genericity = up[1] switch
            {
                '0' => "SAE generic",
                '1' or '2' or '3' => "manufacturer-specific",
                _ => "unspecified"
            };

            var subsystem = up[2] switch
            {
                '0' or '1' => "Fuel and Air Metering",
                '2' => "Fuel and Air Metering (Injector Circuit)",
                '3' => "Ignition System or Misfire",
                '4' => "Auxiliary Emission Controls",
                '5' => "Vehicle Speed and Idle Control",
                '6' => "Computer Output Circuit",
                '7' or '8' => "Transmission",
                _ => "Subsystem"
            };

            var title = $"{system} - {subsystem} ({genericity})";
            info = new DtcInfo(up, title, DtcInfoUtil.BuildInfoUrl(up));
            return true;
        }
    }

    internal sealed class WebDtcInfoProvider : IDtcInfoProvider
    {
        private readonly string _listUrl;
        private readonly string _listCachePath;
        private readonly string[] _codePageTemplates;
        private readonly string _codeCacheDir;
        private readonly TimeSpan _cacheMaxAge;

        private static volatile bool s_webBlocked;
        private readonly bool _debug =
            string.Equals(Environment.GetEnvironmentVariable("DTC_INFO_DEBUG"), "1", StringComparison.OrdinalIgnoreCase);
        private readonly bool _disabled =
            string.Equals(Environment.GetEnvironmentVariable("DTC_INFO_DISABLE_WEB"), "1", StringComparison.OrdinalIgnoreCase);

        public WebDtcInfoProvider(
            string listUrl,
            string listCachePath,
            TimeSpan cacheMaxAge,
            string[]? codePageTemplates = null,
            string? codeCacheDir = null)
        {
            _listUrl = listUrl;
            _listCachePath = listCachePath;
            _cacheMaxAge = cacheMaxAge;
            _codePageTemplates = codePageTemplates is { Length: > 0 }
                ? codePageTemplates
                : new[]
                {
                    "https://club.autodoc.se/obd-codes/{code}"
                };
            _codeCacheDir = codeCacheDir ?? Path.Combine(Path.GetDirectoryName(listCachePath) ?? ".", "dtc-cache");
        }

        public bool TryGetInfo(string code, out DtcInfo info)
        {
            if (_disabled || s_webBlocked)
            {
                if (_debug && _disabled) Console.WriteLine("[DTC] WebDtcInfoProvider disabled by env (DTC_INFO_DISABLE_WEB=1)");
                if (_debug && s_webBlocked) Console.WriteLine("[DTC] WebDtcInfoProvider is blocked (previous 403/401). Skipping web.");
                info = new DtcInfo(code, null, _listUrl);
                return false;
            }

            var up = code.ToUpperInvariant();

            var listHtml = LoadHtml(_listUrl, _listCachePath, _cacheMaxAge, referer: null, out var listFromCache);
            if (_debug) LogHtmlStats("list", _listCachePath, listHtml, up);
            if (!s_webBlocked)
            {
                var discovered = TryDiscoverPerCodeUrl(listHtml, up);
                if (!string.IsNullOrWhiteSpace(discovered))
                {
                    var html = LoadCodePage(discovered, up, out var fromCache);
                    if (_debug) LogHtmlStats("code", discovered, html, up);
                    var desc = TryExtractFromCodePage(html, up);
                    if (!string.IsNullOrWhiteSpace(desc))
                    {
                        if (_debug) Console.WriteLine($"[DTC] WebDtcInfoProvider using discovered link: {discovered} (fromCache={fromCache}, listFromCache={listFromCache})");
                        info = new DtcInfo(up, desc, discovered);
                        return true;
                    }
                }
            }

            if (!s_webBlocked)
            {
                foreach (var tpl in _codePageTemplates)
                {
                    foreach (var lower in new[] { true, false })
                    {
                        var token = lower ? up.ToLowerInvariant() : up;
                        var url = tpl.Replace("{code}", token);
                        var html = LoadCodePage(url, up, out var fromCache);
                        if (_debug) LogHtmlStats("code", url, html, up);
                        var desc = TryExtractFromCodePage(html, up);
                        if (!string.IsNullOrWhiteSpace(desc))
                        {
                            if (_debug) Console.WriteLine($"[DTC] WebDtcInfoProvider using template link: {url} (fromCache={fromCache})");
                            info = new DtcInfo(up, desc, url);
                            return true;
                        }
                    }
                }
            }

            var listDesc = TryExtractFromList(listHtml, up);
            if (!string.IsNullOrWhiteSpace(listDesc))
            {
                if (_debug) Console.WriteLine($"[DTC] WebDtcInfoProvider using list page text for {up} (fromCache={listFromCache})");
                info = new DtcInfo(up, listDesc, _listUrl);
                return true;
            }

            if (_debug) Console.WriteLine($"[DTC] WebDtcInfoProvider found no info for {up}");
            info = new DtcInfo(up, null, _listUrl);
            return false;
        }

        private static void LogHtmlStats(string kind, string source, string html, string code)
        {
            try
            {
                var len = string.IsNullOrEmpty(html) ? 0 : html.Length;
                var hasCode = !string.IsNullOrEmpty(html) && html.IndexOf(code, StringComparison.OrdinalIgnoreCase) >= 0;
                Console.WriteLine($"[DTC] Web {kind} html: {source} (len={len}, containsCode={hasCode})");
            }
            catch { }
        }

        private static string LoadHtml(string url, string cachePath, TimeSpan cacheMaxAge, string? referer, out bool fromCache)
        {
            fromCache = false;
            var debug = string.Equals(Environment.GetEnvironmentVariable("DTC_INFO_DEBUG"), "1", StringComparison.OrdinalIgnoreCase);
            try
            {
                if (File.Exists(cachePath))
                {
                    var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath);
                    if (age <= cacheMaxAge)
                    {
                        fromCache = true;
                        return File.ReadAllText(cachePath);
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                using var http = DtcInfoUtil.CreateHttp(referer);
                using var resp = http.GetAsync(url).GetAwaiter().GetResult();
                var content = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (debug)
                    Console.WriteLine($"[DTC] HTTP {((int)resp.StatusCode)} {resp.ReasonPhrase} for {url}, len={content?.Length ?? 0}");

                if (resp.StatusCode == HttpStatusCode.Forbidden || resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    s_webBlocked = true;
                    return string.Empty;
                }

                if (resp.IsSuccessStatusCode && !string.IsNullOrEmpty(content))
                {
                    File.WriteAllText(cachePath, content);
                    return content;
                }

                if (File.Exists(cachePath))
                {
                    fromCache = true;
                    return File.ReadAllText(cachePath);
                }
                return string.Empty;
            }
            catch
            {
                try
                {
                    if (File.Exists(cachePath))
                    {
                        fromCache = true;
                        return File.ReadAllText(cachePath);
                    }
                }
                catch { }
                return string.Empty;
            }
        }

        private string LoadCodePage(string url, string code, out bool fromCache)
        {
            var safe = code.ToLowerInvariant();
            var cachePath = Path.Combine(_codeCacheDir, $"dtc-{safe}.html");
            return LoadHtml(url, cachePath, _cacheMaxAge, referer: _listUrl, out fromCache);
        }

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