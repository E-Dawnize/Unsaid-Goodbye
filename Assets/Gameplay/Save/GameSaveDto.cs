using System;
using System.Collections.Generic;
using Gameplay.SceneFlow;
using UnityEngine;

namespace Gameplay.Save
{
    /// <summary>
    /// 纯数据存档 DTO — JSON 序列化用，不继承 ScriptableObject
    /// </summary>
    [Serializable]
    public class GameSaveDto
    {
        public GamePhase currentPhase;
        public string currentSceneId;
        public List<string> completedBeatIds = new();
        public List<string> collectedItemIds = new();
        public List<string> solvedPuzzleIds = new();
        public List<string> completedDialogueIds = new();
        public float playTimeSeconds;
        public string saveDateTime;

        public static GameSaveDto CreateDefault()
        {
            return new GameSaveDto
            {
                currentPhase = GamePhase.Phase1_SurfaceLivingRoom_Initial,
                currentSceneId = string.Empty,
                saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                playTimeSeconds = 0f
            };
        }
    }
}
