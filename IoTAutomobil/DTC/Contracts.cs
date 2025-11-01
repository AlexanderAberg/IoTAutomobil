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
}