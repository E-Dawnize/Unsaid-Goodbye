using System.IO;
using UnityEngine;

namespace Gameplay.Save
{
    public class SaveManager : ISaveManager
    {
        private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        public bool SaveExists()
        {
            return File.Exists(SavePath);
        }

        public GameSaveDto LoadSave()
        {
            if (!SaveExists())
            {
                Debug.LogWarning("[SaveManager] No save file found, creating default.");
                return CreateNewSave();
            }

            var json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<GameSaveDto>(json);
            Debug.Log($"[SaveManager] Save loaded from: {SavePath}, phase={data.currentPhase}");
            return data;
        }

        public GameSaveDto CreateNewSave()
        {
            var data = GameSaveDto.CreateDefault();
            WriteSave(data);
            Debug.Log($"[SaveManager] New save created at: {SavePath}");
            return data;
        }

        public void WriteSave(GameSaveDto data)
        {
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] Save written to: {SavePath}");
        }
    }
}
