using System.Collections.Generic;
using Gameplay.SceneFlow;
using UnityEngine;

namespace Gameplay.SO
{
    /// <summary>
    /// 存档SO
    /// </summary>
    [CreateAssetMenu(fileName = "GameSaveData", menuName = "SO/GameSaveData")]
    public class GameSaveData:ScriptableObject
    {
        public GamePhase currentPhase;
        public string currentSceneId;
        public List<string> completedBeatIds;          // 当前阶段已完成的Beat
        public List<string> collectedItemIds;           // 全部已收集道具
        public List<string> solvedPuzzleIds;            // 全部已解谜
        public List<string> completedDialogueIds;       // 全部已完成对话
        public float playTimeSeconds;
        public string saveDateTime;
    }
    public class GameSaveDataRuntime
    {
        public GamePhase CurrentPhase;
        public string CurrentSceneId;
        public HashSet<string> CompletedBeatIds=new ();          // 当前阶段已完成的Beat
        public HashSet<string> CollectedItemIds=new ();           // 全部已收集道具
        public HashSet<string> SolvedPuzzleIds=new ();            // 全部已解谜
        public HashSet<string> CompletedDialogueIds=new ();       // 全部已完成对话
        public GameSaveDataRuntime(GameSaveData gameSaveData)
        {
            CurrentPhase = gameSaveData.currentPhase;
            CurrentSceneId=gameSaveData.currentSceneId;
            foreach (var completedBeatId in gameSaveData.completedBeatIds)
            {
                CompletedBeatIds.Add(completedBeatId);
            }

            foreach (var completedDialogueId in gameSaveData.completedDialogueIds)
            {
                CompletedDialogueIds.Add(completedDialogueId);
            }

            foreach (var solvedPuzzleId in gameSaveData.solvedPuzzleIds)
            {
                SolvedPuzzleIds.Add(solvedPuzzleId);
            }

            foreach (var collectedItemId in gameSaveData.collectedItemIds)
            {
                CollectedItemIds.Add(collectedItemId);
            }
        }
    }
}