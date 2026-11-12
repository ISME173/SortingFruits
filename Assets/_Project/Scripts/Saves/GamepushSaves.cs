using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using GamePush;
using GamePush.Native;
using Newtonsoft.Json;
using UnityEngine;
using PlayerPrefs = UnityEngine.PlayerPrefs;

namespace _Project.Scripts.Saves
{
    public class GamepushSaves : ISaves
    {
        private const string TrueKey = "true", FalseKey = "false";
        private const string BlobKey = "game_saves";
        private const string LocalBlobKey = "gp.game_saves";
        private const string EnvelopePrefix = "gp1:";

        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();
        private bool _loaded;
        private bool _acceptCloud;

        public GamepushSaves()
        {
            GP_Storage.OnGetValue += OnStorageGet;
        }

        public void ClearAllSaves()
        {
            EnsureLoaded();
            _values.Clear();
            _acceptCloud = false;
            PlayerPrefs.DeleteAll();
            StoreLocal();
            PlayerPrefs.Save();
            PushRemote();
            GP_Player.Sync();
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            EnsureLoaded();
            return PlayerPrefs.GetString(key, defaultValue == false ? FalseKey : TrueKey) == TrueKey ? true : false;
        }

        public float GetFloat(string key, float defaultValue = 0)
        {
            EnsureLoaded();
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            EnsureLoaded();
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public string GetString(string key, string defaultValue = "")
        {
            EnsureLoaded();
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public bool HasKey(string key)
        {
            EnsureLoaded();
            return PlayerPrefs.HasKey(key);
        }

        public void Save()
        {
            EnsureLoaded();
            StoreLocal();
            PlayerPrefs.Save();
            PushRemote();
            GP_Player.Sync();
        }

        public void SetBool(string key, bool value)
        {
            EnsureLoaded();
            var raw = value ? TrueKey : FalseKey;
            PlayerPrefs.SetString(key, raw);
            Put(key, Pack('s', raw));
        }

        public void SetFloat(string key, float value)
        {
            EnsureLoaded();
            PlayerPrefs.SetFloat(key, value);
            Put(key, Pack('f', value.ToString(CultureInfo.InvariantCulture)));
        }

        public void SetInt(string key, int value)
        {
            EnsureLoaded();
            PlayerPrefs.SetInt(key, value);
            Put(key, Pack('i', value.ToString(CultureInfo.InvariantCulture)));
        }

        public void SetString(string key, string value)
        {
            EnsureLoaded();
            var raw = value ?? string.Empty;
            PlayerPrefs.SetString(key, raw);
            Put(key, Pack('s', raw));
        }

        public void SetObject<T>(string key, T value, bool prettyPrint = true)
        {
            EnsureLoaded();

            if (value == null)
            {
                var removed = _values.Remove(key);
                if (!removed && !PlayerPrefs.HasKey(key))
                    return;

                PlayerPrefs.DeleteKey(key);
                StoreLocal();
                return;
            }

            var formatting = prettyPrint ? Formatting.Indented : Formatting.None;
            var json = JsonConvert.SerializeObject(value, formatting);
            PlayerPrefs.SetString(key, json);
            Put(key, Pack('s', json));
        }

        public T GetObject<T>(string key, T defaultValue = default)
        {
            EnsureLoaded();

            if (!PlayerPrefs.HasKey(key))
                return defaultValue;

            var json = PlayerPrefs.GetString(key, null);
            if (string.IsNullOrWhiteSpace(json))
                return defaultValue;

            try
            {
                var result = JsonConvert.DeserializeObject<T>(json);
                return result == null ? defaultValue : result;
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task<T> GetObjectAsync<T>(string key, T defaultValue = default, CancellationToken cancellationToken = default)
        {
            EnsureLoaded();

            if (!PlayerPrefs.HasKey(key))
                return defaultValue;

            var json = PlayerPrefs.GetString(key, null);
            if (string.IsNullOrWhiteSpace(json))
                return defaultValue;

            try
            {
                var result = await Task.Run(() => JsonConvert.DeserializeObject<T>(json), cancellationToken);
                return result == null ? defaultValue : result;
            }
            catch
            {
                return defaultValue;
            }
        }

        private void Put(string key, string packed)
        {
            _values[key] = packed;
            StoreLocal();
        }

        private void StoreLocal()
        {
            _acceptCloud = false;
            PlayerPrefs.SetString(LocalBlobKey, EnvelopePrefix + JsonConvert.SerializeObject(_values));
        }

        private static void PushRemote()
        {
            var envelope = PlayerPrefs.GetString(LocalBlobKey, EnvelopePrefix + "{}");
            GP_Player.Set(BlobKey, envelope);
            GP_Storage.Set(BlobKey, envelope);
        }

        private void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;

            if (TryReadLocal())
                return;

            if (TryImport(ReadPlayerBlob()))
                return;

            _acceptCloud = true;
#if !UNITY_EDITOR && UNITY_WEBGL
            GP_Storage.Get(BlobKey, null);
#endif
        }

        private bool TryReadLocal()
        {
            if (!TryParse(PlayerPrefs.GetString(LocalBlobKey, null), out var data))
                return false;

            _values.Clear();
            foreach (var pair in data)
            {
                if (!string.IsNullOrEmpty(pair.Key) && PlayerPrefs.HasKey(pair.Key))
                    _values[pair.Key] = pair.Value;
            }

            return true;
        }

        private bool TryImport(string raw)
        {
            if (!TryParse(raw, out var data))
                return false;

            _values.Clear();
            foreach (var pair in data)
            {
                if (string.IsNullOrEmpty(pair.Key))
                    continue;

                if (PlayerPrefs.HasKey(pair.Key))
                {
                    if (TryCaptureLocal(pair.Key, pair.Value, out var localPacked))
                        _values[pair.Key] = localPacked;
                    continue;
                }

                if (!ApplyPacked(pair.Key, pair.Value))
                    continue;

                _values[pair.Key] = pair.Value;
            }

            PlayerPrefs.SetString(LocalBlobKey, EnvelopePrefix + JsonConvert.SerializeObject(_values));
            PlayerPrefs.Save();
            _acceptCloud = false;
            return true;
        }

        private void OnStorageGet(StorageField field)
        {
            if (!_acceptCloud || field == null || field.key != BlobKey)
                return;

            if (PlayerPrefs.HasKey(LocalBlobKey))
                return;

            TryImport(field.value);
        }

        private static string ReadPlayerBlob()
        {
            if (GamePushHost.UseNativeCore)
                return GP_Player.GetString(BlobKey);

#if !UNITY_EDITOR && UNITY_WEBGL && !GP_NATIVE_WEBGL
            if (!GP_Player.Has(BlobKey))
                return null;

            return GP_Player.GetString(BlobKey);
#else
            if (!GP_Prefs.HasKey(BlobKey))
                return null;

            return GP_Player.GetString(BlobKey);
#endif
        }

        private static bool TryCaptureLocal(string key, string packedSample, out string packed)
        {
            packed = null;
            if (!TryUnpack(packedSample, out var type, out _))
                return false;

            switch (type)
            {
                case 'i':
                    packed = Pack('i', PlayerPrefs.GetInt(key).ToString(CultureInfo.InvariantCulture));
                    return true;
                case 'f':
                    packed = Pack('f', PlayerPrefs.GetFloat(key).ToString(CultureInfo.InvariantCulture));
                    return true;
                case 's':
                    packed = Pack('s', PlayerPrefs.GetString(key, string.Empty));
                    return true;
                default:
                    return false;
            }
        }

        private static bool ApplyPacked(string key, string packed)
        {
            if (!TryUnpack(packed, out var type, out var raw))
                return false;

            switch (type)
            {
                case 'i':
                    if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                        return false;

                    PlayerPrefs.SetInt(key, intValue);
                    return true;
                case 'f':
                    if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                        return false;

                    PlayerPrefs.SetFloat(key, floatValue);
                    return true;
                case 's':
                    PlayerPrefs.SetString(key, raw);
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParse(string raw, out Dictionary<string, string> data)
        {
            data = null;
            var json = Unwrap(raw);
            if (json == null)
                return false;

            try
            {
                data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GamepushSaves] Не удалось прочитать сохранения: {exception.Message}");
                return false;
            }
        }

        private static string Unwrap(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || !raw.StartsWith(EnvelopePrefix, StringComparison.Ordinal))
                return null;

            var json = raw.Substring(EnvelopePrefix.Length);
            return json.Length > 0 && json[0] == '{' ? json : null;
        }

        private static string Pack(char type, string value) => type + ":" + (value ?? string.Empty);

        private static bool TryUnpack(string packed, out char type, out string value)
        {
            type = default;
            value = null;

            if (string.IsNullOrEmpty(packed) || packed.Length < 2 || packed[1] != ':')
                return false;

            type = packed[0];
            value = packed.Substring(2);
            return true;
        }
    }
}
