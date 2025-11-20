namespace _Project.Scripts.Saves
{
    public interface ISaves
    {
        public void Save();
        public void ClearAllSaves();

        public void SetInt(string key, int value);
        public void SetFloat(string key, float value);
        public void SetString(string key, string value);
        public void SetBool(string key, bool value);
        public void SetObject<T>(string key, T value, bool prettyPrint = false);

        public int GetInt(string key, int defaultValue = 0);
        public float GetFloat(string key, float defaultValue = 0);
        public string GetString(string key, string defaultValue = "");
        public bool GetBool(string key, bool defaultValue = false);
        public T GetObject<T>(string key, T defaultValue = default);

        public bool HasKey(string key);
    }
}
