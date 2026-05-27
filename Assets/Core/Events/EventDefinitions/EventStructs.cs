using Core.Identity;

// 交互事件 — 所有交互物触发同一事件，消费者从 InteractionDef 各取所需
struct InteractionEvent
{
    public InteractionDef Def;
}

// 对话结束
struct DialogueEndedEvent
{
    public InteractionDef Def;
}

struct StoryBeatCompletedEvent
{
    public string StoryBeatID;
}

// 场景加载请求 — InteractableObject 触发时附带场景切换
struct SceneLoadRequest
{
    public string ScenePath;
    public string BgmAddress;
}

/// <summary>
/// 游戏启动就绪事件 — Boot 序列全部完成后发布一次。
/// 订阅方可在此时执行依赖全局状态的初始化（如播放开场 BGM、显示 HUD）。
/// </summary>
struct GameReadyEvent
{
}
