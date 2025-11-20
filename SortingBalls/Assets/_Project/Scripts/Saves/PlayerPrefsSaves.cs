using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayerPrefs = UnityEngine.PlayerPrefs;

namespace _Project.Scripts.Saves
{
    public class PlayerPrefsSaves : ISaves
    {
        private const string TrueKey = "true", FalseKey = "false";

        public void ClearAllSaves()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return PlayerPrefs.GetString(key, defaultValue == false ? FalseKey : TrueKey) == TrueKey ? true : false;
        }

        public float GetFloat(string key, float defaultValue = 0)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public string GetString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }

        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetString(key, value ? TrueKey : FalseKey);
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public void SetObject<T>(string key, T value, bool prettyPrint = true)
        {
            if (value == null)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            var formatting = prettyPrint ? Formatting.Indented : Formatting.None;
            var json = JsonConvert.SerializeObject(value, formatting);
            PlayerPrefs.SetString(key, json);
        }

        public T GetObject<T>(string key, T defaultValue = default)
        {
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
    }
}
