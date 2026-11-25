mergeInto(LibraryManager.library, {
  UC_StorageAvailable: function () {
    try {
      if (typeof qg !== "undefined" && typeof qg.getStorageSync === "function" && typeof qg.setStorageSync === "function") return 1;
      if (typeof tt !== "undefined" && typeof tt.getStorageSync === "function" && typeof tt.setStorageSync === "function") return 1;
      if (typeof wx !== "undefined" && typeof wx.getStorageSync === "function" && typeof wx.setStorageSync === "function") return 1;
      if (typeof window !== "undefined" && window.localStorage) return 1;
    } catch (e) {}
    return 0;
  },

  UC_StorageSetString: function (keyPtr, valPtr) {
    var k = UTF8ToString(keyPtr);
    var v = UTF8ToString(valPtr);
    try {
      if (typeof qg !== "undefined" && typeof qg.setStorageSync === "function") {
        qg.setStorageSync(k, v);
        return 1;
      }
      if (typeof tt !== "undefined" && typeof tt.setStorageSync === "function") {
        tt.setStorageSync(k, v);
        return 1;
      }
      if (typeof wx !== "undefined" && typeof wx.setStorageSync === "function") {
        wx.setStorageSync(k, v);
        return 1;
      }
      if (typeof window !== "undefined" && window.localStorage) {
        window.localStorage.setItem(k, v);
        return 1;
      }
    } catch (e) {
      console.warn("[UnityCoreStorage] Set failed for key:", k, e);
    }
    return 0;
  },

  UC_StorageGetString: function (keyPtr, fallbackPtr) {
    var k = UTF8ToString(keyPtr);
    var fallback = UTF8ToString(fallbackPtr);
    var result = null;
    try {
      if (typeof qg !== "undefined" && typeof qg.getStorageSync === "function") {
        result = qg.getStorageSync(k);
      } else if (typeof tt !== "undefined" && typeof tt.getStorageSync === "function") {
        result = tt.getStorageSync(k);
      } else if (typeof wx !== "undefined" && typeof wx.getStorageSync === "function") {
        result = wx.getStorageSync(k);
      } else if (typeof window !== "undefined" && window.localStorage) {
        result = window.localStorage.getItem(k);
      }
    } catch (e) {
      console.warn("[UnityCoreStorage] Get failed for key:", k, e);
    }
    var str;
    if (result === null || result === undefined || result === "") {
      str = fallback;
    } else {
      str = typeof result === "string" ? result : String(result);
    }
    var size = lengthBytesUTF8(str) + 1;
    var buf = _malloc(size);
    stringToUTF8(str, buf, size);
    return buf;
  },

  UC_StorageDeleteKey: function (keyPtr) {
    var k = UTF8ToString(keyPtr);
    try {
      if (typeof qg !== "undefined" && typeof qg.removeStorageSync === "function") {
        qg.removeStorageSync(k);
        return;
      }
      if (typeof tt !== "undefined" && typeof tt.removeStorageSync === "function") {
        tt.removeStorageSync(k);
        return;
      }
      if (typeof wx !== "undefined" && typeof wx.removeStorageSync === "function") {
        wx.removeStorageSync(k);
        return;
      }
      if (typeof window !== "undefined" && window.localStorage) {
        window.localStorage.removeItem(k);
      }
    } catch (e) {
      console.warn("[UnityCoreStorage] Delete failed for key:", k, e);
    }
  }
});

