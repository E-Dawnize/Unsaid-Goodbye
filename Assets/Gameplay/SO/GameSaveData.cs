using System.Collections.Generic;
using System.Linq;
using Gameplay.Save;
using Gameplay.SceneFlow;

namespace Gameplay.SO
{
    /// <summary>
    /// 运行时存档数据 — 用 HashSet 保证 O(1) 查找
    /// 从 SaveManager 的 GameSaveDto 构造 / 导出
    /// </summary>
    public class GameSaveDataRuntime
    {
        public GamePhase CurrentPhase;
        public string CurrentSceneId;
        public HashSet<string> CompletedBeatIds = new();
        public HashSet<string> CollectedItemIds = new();
        public HashSet<string> SolvedPuzzleIds = new();
        public HashSet<string> CompletedDialogueIds = new();

        public GameSaveDataRuntime(GameSaveDto dto)
        {
            CurrentPhase = dto.currentPhase;
            CurrentSceneId = dto.currentSceneId;
            CompletedBeatIds = new HashSet<string>(dto.completedBeatIds ?? Enumerable.Empty<string>());
            CollectedItemIds = new HashSet<string>(dto.collectedItemIds ?? Enumerable.Empty<string>());
            SolvedPuzzleIds = new HashSet<string>(dto.solvedPuzzleIds ?? Enumerable.Empty<string>());
            CompletedDialogueIds = new HashSet<string>(dto.completedDialogueIds ?? Enumerable.Empty<string>());
        }

        public GameSaveDto ToDto()
        {
            return new GameSaveDto
            {
                currentPhase = CurrentPhase,
                currentSceneId = CurrentSceneId,
                completedBeatIds = CompletedBeatIds.ToList(),
                collectedItemIds = CollectedItemIds.ToList(),
                solvedPuzzleIds = SolvedPuzzleIds.ToList(),
                completedDialogueIds = CompletedDialogueIds.ToList()
            };
        }
    }
}