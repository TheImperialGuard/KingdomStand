#if UNITY_WEBGL
using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.DataRepository
{
    // Хранилище в localStorage браузера. Работает только в WebGL-сборке: в редакторе функций .jslib нет.
    // Префикс отделяет ключи игры от ключей других игр, открытых с того же адреса.
    public class BrowserStorageDataRepository : IDataRepository
    {
        private readonly string _keysPrefix;

        public BrowserStorageDataRepository(string keysPrefix)
        {
            _keysPrefix = keysPrefix;
        }

        public IEnumerator Exists(string key, Action<bool> onExistsResult)
        {
            bool exists = BrowserStorage_HasKey(FullKeyFor(key));

            onExistsResult?.Invoke(exists);

            yield break;
        }

        public IEnumerator Read(string key, Action<string> onRead)
        {
            string text = BrowserStorage_GetValue(FullKeyFor(key));

            onRead?.Invoke(text);

            yield break;
        }

        public IEnumerator Remove(string key)
        {
            BrowserStorage_DeleteKey(FullKeyFor(key));

            yield break;
        }

        public IEnumerator Write(string key, string serializedData)
        {
            string fullKey = FullKeyFor(key);

            if (BrowserStorage_SetValue(fullKey, serializedData) == false)
                Debug.LogError($"Не удалось записать {fullKey} в localStorage браузера");

            yield break;
        }

        private string FullKeyFor(string key) => _keysPrefix + key;

        [DllImport("__Internal")]
        private static extern bool BrowserStorage_HasKey(string key);

        [DllImport("__Internal")]
        private static extern string BrowserStorage_GetValue(string key);

        [DllImport("__Internal")]
        private static extern bool BrowserStorage_SetValue(string key, string value);

        [DllImport("__Internal")]
        private static extern void BrowserStorage_DeleteKey(string key);
    }
}
#endif
