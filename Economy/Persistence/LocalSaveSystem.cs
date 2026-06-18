using UnityEngine;

namespace Dystopia.Economy.Persistence
{
    public class LocalSaveSystem : ISaveSystem
    {
        public void Save(string key, string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public string Load(string key)
        {
            return PlayerPrefs.GetString(key, null);
        }

        public bool Exists(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}