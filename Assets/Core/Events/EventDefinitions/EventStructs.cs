using UnityEngine;

// 收集道具
struct ItemCollectedEvent
{
    public string ItemID;
} 
// 完成解谜
struct PuzzleSolvedEvent
{
    public string PuzzleID;
}
// 完成对话
struct DialogueEndedEvent
{
    public string DialogueID;
}
// 交互特定物体
struct InteractionPerformedEvent
{
    public string InteractableID;    
}
// 进入触发区域
struct TriggerEnterEvent
{
    public string TriggerID;
}

struct StoryBeatCompletedEvent
{
    public string StoryBeatID;
}