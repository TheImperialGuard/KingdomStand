var BrowserStorageLibrary = {

  // Доступ к localStorage может бросить исключение: хранилище отключено в браузере или запрещено во фрейме.

  BrowserStorage_HasKey: function (key) {
    try {
      return localStorage.getItem(UTF8ToString(key)) !== null ? 1 : 0;
    }
    catch (e) {
      console.error('BrowserStorage: has key error: ', e.message);
      return 0;
    }
  },

  // Буфер строки освобождает IL2CPP после передачи в C#.
  BrowserStorage_GetValue: function (key) {
    try {
      var value = localStorage.getItem(UTF8ToString(key));

      if (value === null)
        return 0;

      var bufferSize = lengthBytesUTF8(value) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(value, buffer, bufferSize);
      return buffer;
    }
    catch (e) {
      console.error('BrowserStorage: get value error: ', e.message);
      return 0;
    }
  },

  BrowserStorage_SetValue: function (key, value) {
    try {
      localStorage.setItem(UTF8ToString(key), UTF8ToString(value));
      return 1;
    }
    catch (e) {
      console.error('BrowserStorage: set value error: ', e.message);
      return 0;
    }
  },

  BrowserStorage_DeleteKey: function (key) {
    try {
      localStorage.removeItem(UTF8ToString(key));
    }
    catch (e) {
      console.error('BrowserStorage: delete key error: ', e.message);
    }
  }
};

mergeInto(LibraryManager.library, BrowserStorageLibrary);
