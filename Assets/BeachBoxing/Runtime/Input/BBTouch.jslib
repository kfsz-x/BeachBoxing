mergeInto(LibraryManager.library, {
  BB_HasQGTouch: function () {
    if (typeof qg !== "undefined" && typeof qg.onTouchStart === "function") return 1;
    if (typeof wx !== "undefined" && typeof wx.onTouchStart === "function") return 1;
    if (typeof tt !== "undefined" && typeof tt.onTouchStart === "function") return 1;
    if (typeof navigator === "undefined") return 0;
    return /Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent || "") ||
      (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1) ? 1 : 0;
  }
});

