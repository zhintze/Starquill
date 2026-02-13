using UnityEngine;

namespace Starquill.Managers
{
    public class SaveManager : MonoBehaviour
    {
        private const string SaveKey = "StarquillSave";
        public SaveData CurrentSave { get; private set; } = new();

        public void Save()
        {
            CurrentSave.lastPlayedTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string json = JsonUtility.ToJson(CurrentSave, true);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        public SaveData Load()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                string json = PlayerPrefs.GetString(SaveKey);
                CurrentSave = JsonUtility.FromJson<SaveData>(json);
            }
            else
            {
                CurrentSave = new SaveData();
            }
            return CurrentSave;
        }

        public float GetOfflineSeconds()
        {
            if (CurrentSave.lastPlayedTimestamp == 0) return 0;
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now - CurrentSave.lastPlayedTimestamp;
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            CurrentSave = new SaveData();
        }
    }
}
