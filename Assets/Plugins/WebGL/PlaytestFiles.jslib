mergeInto(LibraryManager.library, {
  TwmSyncPlaytestFiles: function () {
    // Coalesce overlapping writes; sync once more if an event arrives mid-sync.
    Module.twmPlaytestSyncDirty = true;
    if (Module.twmPlaytestSyncBusy) return;
    var sync = function () {
      Module.twmPlaytestSyncBusy = true;
      Module.twmPlaytestSyncDirty = false;
      FS.syncfs(false, function (error) {
        Module.twmPlaytestSyncBusy = false;
        if (error) {
          console.error("[Playtest] Browser storage failed. Export CSV before closing.", error);
          SendMessage("GameManager", "OnBrowserStorageError", "Browser storage failed. Export playtest data before closing.");
        }
        if (Module.twmPlaytestSyncDirty) sync();
      });
    };
    sync();
  },
  TwmDownloadPlaytestCsv: function (name, content) {
    var blob = new Blob([UTF8ToString(content)], { type: "text/csv;charset=utf-8" });
    var url = URL.createObjectURL(blob);
    var link = document.createElement("a");
    link.href = url;
    link.download = UTF8ToString(name);
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(function () { URL.revokeObjectURL(url); }, 10000);
  }
});
