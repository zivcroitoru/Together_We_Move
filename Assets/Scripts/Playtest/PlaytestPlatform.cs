using System.IO;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>Native builds use ordinary local files. WebGL additionally syncs
/// the browser filesystem and offers CSV as a download from the button click.</summary>
public static class PlaytestPlatform
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void TwmSyncPlaytestFiles();
    [DllImport("__Internal")] private static extern void TwmDownloadPlaytestCsv(string name, string content);
#endif
    public static void SyncBrowserStorage()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TwmSyncPlaytestFiles();
#endif
    }

    public static void OfferCsvDownload(string path)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TwmDownloadPlaytestCsv(Path.GetFileName(path), "\uFEFF" + File.ReadAllText(path));
#endif
    }
}
