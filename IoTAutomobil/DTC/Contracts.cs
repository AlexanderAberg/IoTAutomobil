namespace IoTAutomobil.DTC
{
    internal interface IDtcProvider
    {
        bool TryGetRandom(out string code);
    }

    internal interface IDtcInfoProvider
    {
        bool TryGetInfo(string code, out DtcInfo info);
    }

    internal sealed class DtcInfo(string code, string? title, string url)
    {
        public string Code { get; } = code;
        public string? Title { get; } = title;
        public string Url { get; } = url;
    }
}