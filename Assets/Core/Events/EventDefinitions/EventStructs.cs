using Core.Identity;

// 收集道具
struct ItemCollectedEvent
{
    public InteractableId ItemID;
}
// 完成解谜
struct PuzzleSolvedEvent
{
    public InteractableId PuzzleID;
}
// 完成对话
struct DialogueEndedEvent
{
    public InteractableId DialogueID;
}
// 交互特定物体
struct InteractionPerformedEvent
{
    public InteractableId InteractableID;
}
// 进入触发区域
struct TriggerEnterEvent
{
    public InteractableId TriggerID;
}

struct StoryBeatCompletedEvent
{
    public string StoryBeatID;
}

/// <summary>
/// 游戏启动就绪事件 — Boot 序列全部完成后发布一次。
/// 订阅方可在此时执行依赖全局状态的初始化（如播放开场 BGM、显示 HUD）。
/// </summary>
struct GameReadyEvent
{
}