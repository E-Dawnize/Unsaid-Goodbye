using UnityEngine;

namespace Gameplay.SceneFlow
{
    public enum StoryBeatType
    {
        CollectItem,        // 收集道具 → ItemCollectedEvent
        SolvePuzzle,        // 完成解谜 → PuzzleSolvedEvent
        CompleteDialogue,   // 完成对话 → DialogueEndedEvent
        InteractWithObject, // 交互特定物体 → InteractionPerformedEvent
        EnterTrigger,       // 进入触发区域 → TriggerEnterEvent
    }
    [System.Serializable]
    public class StoryBeat
    {
        public string BeatId;
        public StoryBeatType Type;
        public string TargetId;

        [TextArea(1, 2)]
        public string Description;
    }
}