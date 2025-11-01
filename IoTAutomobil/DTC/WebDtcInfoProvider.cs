using System;
using System.IO;
using System.Net;

namespace IoTAutomobil.DTC
{
    internal sealed partial class WebDtcInfoProvider : IDtcInfoProvider
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
                : new[] { "https://club.autodoc.se/obd-codes/{code}" };
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

        private string LoadCodePage(string url, string code, out bool fromCache)
        {
            var safe = code.ToLowerInvariant();
            var cachePath = Path.Combine(_codeCacheDir, $"dtc-{safe}.html");
            return LoadHtml(url, cachePath, _cacheMaxAge, referer: _listUrl, out fromCache);
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
                    Console.WriteLine($"[DTC] HTTP {(int)resp.StatusCode} {resp.ReasonPhrase} for {url}, len={content?.Length ?? 0}");

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
    }
}