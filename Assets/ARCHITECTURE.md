# 饲主失踪之后 (Unsaid Goodbye) — 架构设计方案

> 版本: 1.0 | 日期: 2026-04-29 | Unity 2D 叙事探索游戏

---

## 目录

1. [总体架构分层](#1-总体架构分层)
2. [命名空间与目录结构](#2-命名空间与目录结构)
3. [各系统详细设计](#3-各系统详细设计)
   - [3.1 配置数据层 (Configs)](#31-配置数据层)
   - [3.2 事件系统 (Events)](#32-事件系统)
   - [3.3 DI/Installer 重组](#33-diinstaller-重组)
   - [3.4 场景流程控制器 (SceneFlow)](#34-场景流程控制器)
   - [3.5 交互系统 (Interaction)](#35-交互系统)
   - [3.6 道具收集系统 (Inventory)](#36-道具收集系统)
   - [3.7 叙事对话系统 (Narrative)](#37-叙事对话系统)
   - [3.8 解谜系统 (Puzzle)](#38-解谜系统)
   - [3.9 音频管理器 (Audio)](#39-音频管理器)
   - [3.10 存档系统 (Save)](#310-存档系统)
   - [3.11 结局判定系统 (Ending)](#311-结局判定系统)
   - [3.12 UI系统](#312-ui系统)
   - [3.13 输入系统重构](#313-输入系统重构)
4. [系统依赖关系图](#4-系统依赖关系图)
5. [二人协作分工方案](#5-二人协作分工方案)
6. [开发阶段规划](#6-开发阶段规划)

---

## 1. 总体架构分层

```
┌──────────────────────────────────────────────────────────────┐
│                       表现层 (Presentation)                    │
│  UI Views/ViewModels │ 场景交互物 │ 解谜UI │ 对话框/选项框    │
├──────────────────────────────────────────────────────────────┤
│                       游戏逻辑层 (Game Logic)                  │
│  交互系统 │ 道具系统 │ 叙事系统 │ 解谜系统 │ 结局判定        │
├──────────────────────────────────────────────────────────────┤
│                       服务层 (Services)                        │
│  场景流程 │ 音频管理 │ 存档系统 │ 输入适配 │ 事件总线        │
├──────────────────────────────────────────────────────────────┤
│                       基础设施层 (Infrastructure)              │
│  DI容器 │ 生命周期管理 │ MVVM绑定 │ 启动引导                  │
└──────────────────────────────────────────────────────────────┘
```

**核心原则：**
- 依赖方向：上层依赖下层，下层绝不引用上层
- 跨模块通信：事件驱动（EventManager）+ DI注入接口
- 配置与逻辑分离：所有数据用 ScriptableObject 配置
- 场景与逻辑解耦：场景只管挂载，逻辑在 Controller/Manager 中

---

## 2. 命名空间与目录结构

```
Assets/
├── Core/                           # 基础设施（保留+微调）
│   ├── Architecture/               # 生命周期、Installer (保留)
│   │   ├── Installers/
│   │   │   ├── CoreInstaller.cs    # 全局服务注册 (重写)
│   │   │   ├── SceneInstaller.cs   # 场景级服务注册 (重写)
│   │   │   └── ...
│   │   ├── Interfaces/
│   │   │   ├── IInitializable.cs
│   │   │   ├── IStartable.cs
│   │   │   └── ITickable.cs
│   │   ├── LifecycleRegistry.cs
│   │   ├── LifecycleMonoBehaviour.cs
│   │   ├── InstallerAsset.cs
│   │   └── InstallerConfig.cs
│   ├── Boot/
│   │   ├── ProjectBootstrap.cs
│   │   ├── ProjectContext.cs       # (微调 Installer 引用)
│   │   ├── UpdateRunner.cs
│   │   └── SceneScopeRunner.cs     # (移除，由 SceneFlow 接管)
│   ├── DI/
│   │   ├── DIContainer.cs          # 保留不动
│   │   └── Inject.cs              # 保留不动
│   ├── Events/
│   │   ├── EventManager.cs         # 保留不动
│   │   ├── EventInterfaces/
│   │   │   └── IEventCenter.cs
│   │   └── EventDefinitions/
│   │       └── GameEvents.cs       # 新：所有游戏事件
│   └── Tools/
│       └── MonoSingleton.cs
│
├── Game/                           # 游戏逻辑层 (全部新建)
│   ├── Configs/                    # ScriptableObject 配置
│   │   ├── ItemData.cs            # 道具配置
│   │   ├── DialogueData.cs        # 对话配置
│   │   ├── PuzzleData.cs          # 解谜配置
│   │   ├── SceneData.cs           # 场景配置
│   │   └── EndingConditionData.cs # 结局条件配置
│   ├── SceneFlow/                  # 场景流程
│   │   ├── ISceneFlowController.cs
│   │   ├── SceneFlowController.cs
│   │   ├── GamePhase.cs
│   │   └── SceneId.cs
│   ├── Interaction/                # 交互系统
│   │   ├── IInteractable.cs
│   │   ├── IInteractionManager.cs
│   │   ├── InteractionManager.cs
│   │   ├── InteractableBase.cs
│   │   └── Interactables/         # 具体交互物实现
│   │       ├── PickupInteractable.cs
│   │       ├── InvestigateInteractable.cs
│   │       ├── PuzzleTriggerInteractable.cs
│   │       └── ...
│   ├── Inventory/                  # 道具收集
│   │   ├── IInventoryManager.cs
│   │   └── InventoryManager.cs
│   ├── Narrative/                  # 叙事对话
│   │   ├── IDialogueManager.cs
│   │   ├── DialogueManager.cs
│   │   ├── DialogueLine.cs
│   │   └── DialogueChoice.cs
│   ├── Puzzle/                     # 解谜系统
│   │   ├── IPuzzle.cs
│   │   ├── PuzzleBase.cs
│   │   ├── PuzzleType.cs
│   │   └── Puzzles/
│   │       ├── PillCalendarPuzzle.cs
│   │       ├── PhotoLightPuzzle.cs
│   │       ├── DiaryFragmentPuzzle.cs
│   │       └── WindChimePuzzle.cs
│   ├── Ending/                     # 结局判定
│   │   ├── IEndingDeterminer.cs
│   │   ├── EndingDeterminer.cs
│   │   └── EndingType.cs
│   └── Save/                       # 存档系统
│       ├── ISaveManager.cs
│       ├── SaveManager.cs
│       └── GameSaveData.cs
│
├── Audio/                          # 音频层 (全部新建)
│   ├── IAudioManager.cs
│   ├── AudioManager.cs
│   └── BGMTrack.cs
│
├── UI/                             # UI层 (全部新建, 基于现有MVVM)
│   ├── Views/
│   │   ├── DialogView.cs
│   │   ├── ChoiceView.cs
│   │   ├── InteractionPromptView.cs
│   │   ├── HUDView.cs
│   │   ├── CollectionView.cs
│   │   └── EndingView.cs
│   ├── ViewModels/
│   │   ├── DialogViewModel.cs
│   │   ├── ChoiceViewModel.cs
│   │   ├── InteractionPromptViewModel.cs
│   │   ├── HUDViewModel.cs
│   │   ├── CollectionViewModel.cs
│   │   └── EndingViewModel.cs
│   └── Components/
│       ├── DialogBox.cs
│       ├── ChoiceButton.cs
│       └── InteractionIndicator.cs
│
├── Input/                          # 输入系统 (重构)
│   ├── InputConfig/
│   │   └── PlayerInputActions.cs  # 重新生成: 移动+交互+菜单
│   ├── InputInterface/
│   │   └── IPlayerInput.cs        # 重写: 移除攻击/跳跃, 增加交互
│   └── Manager/
│       └── PlayerInputManager.cs  # 重写
│
├── MVVM/                           # MVVM基础设施 (保留)
│   ├── Binding/                    # 保留
│   ├── Commands/                   # 保留
│   ├── Interfaces/                 # 保留
│   ├── Model/                      # 移除 ModelBase (改用具体数据模型)
│   ├── View/                       # 清除, 移入 UI/Views
│   └── ViewModel/                  # 清除空壳, 移入 UI/ViewModels
│
└── Resources/                      # Unity Resources 目录
    └── Configs/
        ├── Items/                  # 各道具 ScriptableObject 实例
        ├── Dialogues/              # 各对话 ScriptableObject 实例
        ├── Puzzles/                # 各解谜 ScriptableObject 实例
        ├── Scenes/                 # 场景配置
        ├── EndingConditions/       # 结局条件
        └── BootConfig.asset        # InstallerConfig
```

---

## 3. 各系统详细设计

### 3.1 配置数据层

所有游戏数据定义为 ScriptableObject，编辑时在 Unity Editor 中配置。

```csharp
// Game/Configs/ItemData.cs
namespace Game.Configs
{
    public enum ItemType { Memory, HiddenClue }

    [CreateAssetMenu(fileName = "Item_", menuName = "UnsaidGoodbye/Item")]
    public class ItemData : ScriptableObject
    {
        public string ItemId;           // 如 "L1", "H0"
        public ItemType Type;
        public string DisplayName;      // 显示名称
        [TextArea(3, 6)]
        public string Description;       // 描述文本
        public string ObtainSceneId;     // 获取场景
        public string InteractionPrompt; // 交互提示文字
    }
}
```

```csharp
// Game/Configs/DialogueData.cs
namespace Game.Configs
{
    [CreateAssetMenu(fileName = "Dialogue_", menuName = "UnsaidGoodbye/Dialogue")]
    public class DialogueData : ScriptableObject
    {
        public string DialogueId;
        public List<DialogueLineData> Lines;
    }

    [System.Serializable]
    public struct DialogueLineData
    {
        public string SpeakerName;
        [TextArea(2, 5)]
        public string Text;
        public float AutoAdvanceTime;    // 0 表示手动推进
        public List<DialogueChoiceData> Choices;
    }

    [System.Serializable]
    public struct DialogueChoiceData
    {
        public string ChoiceText;
        public string NextDialogueId;
        public string RequiredItemId;    // 空 = 无条件
    }
}
```

```csharp
// Game/Configs/PuzzleData.cs
namespace Game.Configs
{
    public enum PuzzleType { PillCalendar, PhotoLight, DiaryFragment, WindChime }

    [CreateAssetMenu(fileName = "Puzzle_", menuName = "UnsaidGoodbye/Puzzle")]
    public class PuzzleData : ScriptableObject
    {
        public string PuzzleId;
        public PuzzleType Type;
        public string SceneId;
        public string RewardItemId;      // 完成奖励的道具ID (L或H)
        public string StartDialogueId;   // 触发时的对话
        public string SolvedDialogueId;  // 完成后的对话
    }
}
```

```csharp
// Game/Configs/SceneData.cs
namespace Game.Configs
{
    [CreateAssetMenu(fileName = "SceneData_", menuName = "UnsaidGoodbye/Scene")]
    public class SceneData : ScriptableObject
    {
        public string SceneId;
        public string SceneAssetPath;    // Unity场景路径
        public BGMTrack BackgroundMusic;
        public List<string> AvailableItems;     // 此场景可收集的道具ID
        public List<string> AvailablePuzzles;   // 此场景的解谜ID
        public string EntryDialogueId;          // 进入场景时播放的对话
        public string ExitDialogueId;           // 离开场景时播放的对话
    }
}
```

---

### 3.2 事件系统

扩展现有 `EventManager`，定义游戏专用事件结构体。

```csharp
// Core/Events/EventDefinitions/GameEvents.cs
namespace Core.Events.EventDefinitions
{
    // 道具相关
    public struct ItemCollectedEvent { public string ItemId; public ItemType Type; }
    public struct AllMemoryCollectedEvent { }
    public struct AllHiddenClueCollectedEvent { }

    // 场景流程
    public struct GamePhaseChangedEvent { public GamePhase PreviousPhase; public GamePhase NewPhase; }
    public struct SceneLoadRequestedEvent { public string SceneId; }
    public struct SceneReadyEvent { public string SceneId; }

    // 解谜
    public struct PuzzleStartedEvent { public string PuzzleId; }
    public struct PuzzleSolvedEvent { public string PuzzleId; public string RewardItemId; }

    // 对话
    public struct DialogueStartedEvent { public string DialogueId; }
    public struct DialogueEndedEvent { public string DialogueId; }
    public struct ChoiceMadeEvent { public string DialogueId; public int ChoiceIndex; }

    // 结局
    public struct EndingTriggeredEvent { public EndingType EndingType; }

    // 存档
    public struct GameSavedEvent { }
    public struct GameLoadedEvent { }

    // 交互
    public struct InteractableHoveredEvent { public string InteractableId; }
    public struct InteractableUnhoveredEvent { public string InteractableId; }
    public struct InteractionPerformedEvent { public string InteractableId; }
}
```

---

### 3.3 DI/Installer 重组

**CoreInstaller** — 注册全局单例服务：

```csharp
// Core/Architecture/Installers/CoreInstaller.cs (重写)
public class CoreInstaller : InstallerAsset
{
    public override void Register(DIContainer container)
    {
        // 基础设施
        container.RegisterSingleton<IEventCenter>(new EventManager());

        // 音频
        container.RegisterSingleton<IAudioManager, AudioManager>();

        // 存档
        container.RegisterSingleton<ISaveManager, SaveManager>();

        // 道具
        container.RegisterSingleton<IInventoryManager, InventoryManager>();

        // 结局
        container.RegisterSingleton<IEndingDeterminer, EndingDeterminer>();

        // 场景流程 (全局唯一)
        container.RegisterSingleton<ISceneFlowController, SceneFlowController>();
    }
}
```

**SceneInstaller** — 注册场景级服务：

```csharp
// Core/Architecture/Installers/SceneInstaller.cs (新增)
public class SceneInstaller : InstallerAsset
{
    public override void Register(DIContainer container)
    {
        // 场景级服务
        container.RegisterScoped<IInteractionManager, InteractionManager>();
        container.RegisterScoped<IDialogueManager, DialogueManager>();

        // ViewModels (Scoped, 随场景释放)
        container.RegisterScoped<DialogViewModel>();
        container.RegisterScoped<ChoiceViewModel>();
        container.RegisterScoped<InteractionPromptViewModel>();
        container.RegisterScoped<HUDViewModel>();
        container.RegisterScoped<CollectionViewModel>();
    }
}
```

---

### 3.4 场景流程控制器

管理游戏阶段切换、场景加载、流程推进。

```csharp
// Game/SceneFlow/GamePhase.cs
namespace Game.SceneFlow
{
    public enum GamePhase
    {
        None,
        Phase1_SurfaceLivingRoom_Initial,    // 醒来+初始调查
        Phase1_SurfaceLivingRoom_Investigate,// 深入调查(手机/相框/药瓶)
        Phase2_InnerPark,                    // 里世界·公园
        Phase3_SurfaceLivingRoom_Photo,      // 发现照片
        Phase4_InnerBedroom,                 // 里世界·卧室
        Phase5_InnerBalcony,                 // 里世界·阳台
        Phase6_InnerLivingRoom,              // 镜子+真相
        Phase7_EpilogueA,                    // 结局A·留下
        Phase7_EpilogueB                     // 结局B·离开
    }
}
```

```csharp
// Game/SceneFlow/SceneId.cs
namespace Game.SceneFlow
{
    public static class SceneConstants
    {
        public const string SurfaceLivingRoom = "Surface_LivingRoom";
        public const string SurfaceBalcony = "Surface_Balcony";
        public const string InnerPark = "Inner_Park";
        public const string InnerBedroom = "Inner_Bedroom";
        public const string InnerBalcony = "Inner_Balcony";
        public const string InnerLivingRoom = "Inner_LivingRoom";
        public const string EpilogueA = "Epilogue_A";
        public const string EpilogueB = "Epilogue_B";
    }
}
```

```csharp
// Game/SceneFlow/ISceneFlowController.cs
namespace Game.SceneFlow
{
    public interface ISceneFlowController : IInitializable
    {
        GamePhase CurrentPhase { get; }
        string CurrentSceneId { get; }

        /// 推进到下一阶段（条件满足时调用）
        void AdvancePhase();

        /// 直接跳转到指定阶段（用于存档恢复）
        void JumpToPhase(GamePhase phase);

        /// 请求加载场景
        void RequestSceneLoad(string sceneId);
    }
}
```

**核心流程逻辑（SceneFlowController）：**
```
Phase1_Initial → (收集H0/H1/H2, 靠近猫碗) → Phase1_Investigate
Phase1_Investigate → (靠近阳台) → Phase2_InnerPark
Phase2_InnerPark → (收集L1, 安葬项圈完成) → Phase3_Photo
Phase3_Photo → (照片解谜完成, 收集L2) → Phase4_InnerBedroom
Phase4_InnerBedroom → (收集L3/L4, 安葬橘猫完成) → Phase5_InnerBalcony
Phase5_InnerBalcony → (收集H3, 鹦鹉消散) → Phase6_InnerLivingRoom
Phase6_InnerLivingRoom → (镜子, 结局判定) → Phase7_EpilogueA 或 Phase7_EpilogueB
```

---

### 3.5 交互系统

第一人称点击交互，所有可交互物实现 `IInteractable`。

```csharp
// Game/Interaction/IInteractable.cs
namespace Game.Interaction
{
    public interface IInteractable
    {
        string InteractableId { get; }
        string PromptText { get; }         // 显示给玩家的交互提示
        bool CanInteract { get; }
        bool IsOneShot { get; }            // 是否一次性交互

        void OnInteract();
        void OnHoverEnter();
        void OnHoverExit();
        void OnHoverStay();                // 持续悬停（可选，如光源调整）
    }
}
```

```csharp
// Game/Interaction/IInteractionManager.cs
namespace Game.Interaction
{
    public interface IInteractionManager : IInitializable, ITickable
    {
        IInteractable CurrentHoverTarget { get; }
        void RegisterInteractable(IInteractable interactable);
        void UnregisterInteractable(IInteractable interactable);
    }
}
```

**交互管理流程：**
```
Player 射线/准星 → InteractionManager.Update() 检测最近 IInteractable
→ OnHoverEnter/Exit 切换 → 更新 UI 提示文字
→ 玩家按下交互键 → CurrentHoverTarget.OnInteract()
→ Interactable 内部发事件 (ItemCollected / PuzzleStarted / DialogueStarted)
```

**具体交互物实现示例：**

```csharp
// Game/Interaction/Interactables/PickupInteractable.cs
public class PickupInteractable : InteractableBase
{
    public ItemData ItemData;
    [Inject] private IInventoryManager _inventory;

    public override void OnInteract()
    {
        _inventory.CollectItem(ItemData.ItemId);
        EventCenter.Publish(new ItemCollectedEvent { ItemId = ItemData.ItemId, Type = ItemData.Type });
        gameObject.SetActive(false); // 道具消失
    }
}

// Game/Interaction/Interactables/PuzzleTriggerInteractable.cs
public class PuzzleTriggerInteractable : InteractableBase
{
    public string PuzzleId;
    [Inject] private IPuzzleManager _puzzleManager;

    public override void OnInteract()
    {
        _puzzleManager.StartPuzzle(PuzzleId);
    }
}
```

---

### 3.6 道具收集系统

```csharp
// Game/Inventory/IInventoryManager.cs
namespace Game.Inventory
{
    public interface IInventoryManager : IInitializable
    {
        IReadOnlyList<string> CollectedItems { get; }
        bool HasItem(string itemId);
        void CollectItem(string itemId);
        int MemoryCount { get; }       // L道具数量
        int HiddenClueCount { get; }   // H道具数量
        bool AllMemoriesCollected { get; }
        bool AllHiddenCluesCollected { get; }
        IReadOnlyList<string> GetItemsByType(ItemType type);
    }
}
```

---

### 3.7 叙事对话系统

```csharp
// Game/Narrative/IDialogueManager.cs
namespace Game.Narrative
{
    public interface IDialogueManager : IInitializable
    {
        bool IsPlaying { get; }
        void StartDialogue(string dialogueId);
        void AdvanceLine();             // 推进到下一行
        void MakeChoice(int index);     // 选择选项
        void SkipDialogue();            // 跳过当前对话
    }
}
```

**对话数据流：**
```
触发条件 → DialogueManager.StartDialogue(id)
→ 加载 DialogueData (ScriptableObject)
→ 逐行显示 DialogueLine
  → 有选项时暂停等待 Choice
  → 无选项时等待玩家点击/自动推进
→ 对话结束 → DialogueEndedEvent
```

**对话条件判断：**
```csharp
// DialogueManager 内部
private bool CanStartDialogue(DialogueData data)
{
    foreach (var preq in data.PrerequisiteItemIds)
        if (!_inventory.HasItem(preq)) return false;

    foreach (var preq in data.PrerequisitePuzzleIds)
        if (!_puzzleState.IsSolved(preq)) return false;

    return true;
}
```

---

### 3.8 解谜系统

四种解谜统一接口：

```csharp
// Game/Puzzle/IPuzzle.cs
namespace Game.Puzzle
{
    public interface IPuzzle
    {
        string PuzzleId { get; }
        PuzzleType Type { get; }
        bool IsSolved { get; }
        bool IsActive { get; }

        void Initialize(PuzzleData data);
        void StartPuzzle();
        void ResetPuzzle();
        void Cleanup();
    }
}
```

```csharp
// Game/Puzzle/PuzzleBase.cs
namespace Game.Puzzle
{
    public abstract class PuzzleBase : IPuzzle
    {
        [Inject] protected IEventCenter EventCenter;
        [Inject] protected IInventoryManager Inventory;

        public string PuzzleId { get; private set; }
        public PuzzleType Type { get; private set; }
        public bool IsSolved { get; protected set; }
        public bool IsActive { get; protected set; }

        protected PuzzleData _data;

        public virtual void Initialize(PuzzleData data)
        {
            _data = data;
            PuzzleId = data.PuzzleId;
            Type = data.Type;
        }

        public virtual void StartPuzzle()
        {
            IsActive = true;
            EventCenter.Publish(new PuzzleStartedEvent { PuzzleId = PuzzleId });
        }

        protected virtual void OnSolved()
        {
            IsSolved = true;
            IsActive = false;
            EventCenter.Publish(new PuzzleSolvedEvent
            {
                PuzzleId = PuzzleId,
                RewardItemId = _data.RewardItemId
            });
            if (!string.IsNullOrEmpty(_data.RewardItemId))
                Inventory.CollectItem(_data.RewardItemId);
        }

        public abstract void ResetPuzzle();
        public virtual void Cleanup() { IsActive = false; }
    }
}
```

**四种解谜实现要点：**

| 解谜 | 核心机制 | 输入方式 |
|------|---------|---------|
| PillCalendarPuzzle | 点击日历推算日期，计算23天服药日 | 点击 |
| PhotoLightPuzzle | 拖拽光源角度，光对准烧焦处显文字 | 拖拽/滑动 |
| DiaryFragmentPuzzle | 拖拽3-4张碎片到正确位置，旋转对齐 | 拖拽+旋转 |
| WindChimePuzzle | 3-4音序列记忆小游戏 | 点击（声音按钮） |

```csharp
// Game/Puzzle/Puzzles/PillCalendarPuzzle.cs
public class PillCalendarPuzzle : PuzzleBase
{
    public int CorrectDay = 23;       // 正确答案：第23天
    private int _selectedDay = -1;

    // 点击日历某天时调用
    public void OnDayClicked(int day)
    {
        if (!IsActive || IsSolved) return;
        _selectedDay = day;
        if (day == CorrectDay) OnSolved();
    }
}
```

---

### 3.9 音频管理器

```csharp
// Audio/BGMTrack.cs
namespace Audio
{
    public enum BGMTrack
    {
        M0_Title,
        M1_SurfaceLivingRoom,
        M2_SurfaceBalcony,
        M3_InnerPark,
        M4_InnerBedroom,
        M5_InnerBalcony,
        M6_InnerLivingRoom,
        M7_EpilogueA,
        M8_EpilogueB
    }
}
```

```csharp
// Audio/IAudioManager.cs
namespace Audio
{
    public interface IAudioManager : IInitializable
    {
        void PlayBGM(BGMTrack track, float crossfadeDuration = 1.5f);
        void StopBGM(float fadeOutDuration = 1.0f);
        void PlaySFX(string sfxId);
        void SetMasterVolume(float volume);
        void SetBGMVolume(float volume);
        void SetSFXVolume(float volume);
        float MasterVolume { get; }
        float BGMVolume { get; }
        float SFXVolume { get; }
    }
}
```

**SFX ID 常量（关键音效事件）：**

| SFX ID | 触发事件 |
|--------|---------|
| `sfx_phone_vibrate` | 调查手机 |
| `sfx_sms_alert` | 短信提示音 |
| `sfx_bury_collar` | 安葬项圈（泥土+卡车引擎） |
| `sfx_dog_bark_stop` | 狗吠骤停 |
| `sfx_cat_fade` | 橘猫消散 |
| `sfx_parrot_fade` | 鹦鹉消散 |
| `sfx_mirror_touch` | 触摸镜子 |
| `sfx_transition_breath` | 场景转场呼吸声 |

---

### 3.10 存档系统

```csharp
// Game/Save/GameSaveData.cs
namespace Game.Save
{
    [System.Serializable]
    public class GameSaveData
    {
        public int Version = 1;
        public GamePhase CurrentPhase;
        public string CurrentSceneId;
        public List<string> CollectedItemIds;
        public List<string> SolvedPuzzleIds;
        public List<string> CompletedDialogueIds;
        public float PlayTimeSeconds;
        public string SaveDateTime;
    }
}
```

```csharp
// Game/Save/ISaveManager.cs
namespace Game.Save
{
    public interface ISaveManager : IInitializable
    {
        bool HasSave { get; }
        void Save(GameSaveData data);
        GameSaveData Load();
        void DeleteSave();
    }
}
```

使用 `PlayerPrefs` 存储 JSON（单存档足够），关键保存时机：
- 道具收集后
- 解谜完成后
- 场景转换前
- 对话节点完成后

---

### 3.11 结局判定系统

```csharp
// Game/Ending/EndingType.cs
namespace Game.Ending
{
    public enum EndingType { A_Stay, B_Leave }
}
```

```csharp
// Game/Ending/IEndingDeterminer.cs
namespace Game.Ending
{
    public interface IEndingDeterminer
    {
        EndingType DetermineEnding(IInventoryManager inventory);
        bool CanUnlockEndingA(IInventoryManager inventory);
        bool CanUnlockEndingB(IInventoryManager inventory);
    }
}
```

**判定规则（对应策划案）：**

| 条件 | 结局A (留下) | 结局B (离开) |
|------|:-----------:|:-----------:|
| L1-L4 全收集 | 必须 | 必须 |
| H0 收集 | 不要求 | 必须 |
| H1-H4 全收集 | ≤2个 | 全部4个 |

---

### 3.12 UI系统

基于现有 MVVM 绑定框架构建，复用 `PropertyBinding` / `CommandBinding`。

**核心 UI 组件关系：**

```
HUDView ←→ HUDViewModel
  └─ InteractionPrompt (交互提示文字)
  └─ CollectionIndicator (收集进度)

DialogView ←→ DialogViewModel
  └─ DialogBox (对话框)
  └─ ChoiceView ←→ ChoiceViewModel (选项框)

PuzzleUI (内嵌在场景中，非独立UI)
  └─ 拖拽光源 (PhotoLight)
  └─ 日历点击 (PillCalendar)
  └─ 碎片拖拽 (DiaryFragment)
  └─ 声音记忆按钮 (WindChime)
```

**UI交互按钮清单（对应策划案）：**
```
"调查" "打开" "触碰" "埋进泥土" "阅读"
"叼起" "回头" "检查" "吃几口" "推倒药瓶"
"调整光源" "拼贴碎片"
```
这些作为 `InteractionPromptView` 的动态提示文字，由 `InteractionManager` 当前目标决定。

---

### 3.13 输入系统重构

从战斗输入改为第一人称探索输入：

```csharp
// Input/InputInterface/IPlayerInput.cs (重写)
namespace Input.InputInterface
{
    public interface IPlayerInput
    {
        // 移动
        event Action<Vector2> OnMovePerformed;
        event Action<Vector2> OnMoveCanceled;

        // 视角 (鼠标)
        event Action<Vector2> OnLookPerformed;

        // 交互
        event Action OnInteractPerformed;    // 调查/拾取/使用

        // 菜单
        event Action OnMenuPerformed;        // 打开/关闭菜单

        void OnEnable();
        void OnDisable();
    }
}
```

**键位映射（建议）：**
| 操作 | 键位 |
|------|------|
| 移动 | WASD |
| 视角/光标 | 鼠标移动 |
| 交互 | 鼠标左键 / E |
| 菜单 | Esc |

---

## 4. 系统依赖关系图

```
                    ┌──────────────────┐
                    │   ScriptableObject│
                    │      Configs      │
                    └────────┬─────────┘
                             │ 被所有系统读取
                             ▼
┌──────────┐     ┌──────────────────┐     ┌──────────┐
│  Event   │◄────│   所有 Manager    │────►│   DI     │
│  System  │     └────────┬─────────┘     │Container │
└──────────┘              │               └──────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│SceneFlow     │  │ Interaction  │  │ Inventory    │
│Controller    │  │ Manager      │  │ Manager      │
└──────┬───────┘  └──────┬───────┘  └──────┬───────┘
       │                 │                 │
       │          ┌──────┴───────┐         │
       │          ▼              ▼         │
       │   ┌──────────┐  ┌──────────┐      │
       │   │ Puzzle   │  │Narrative │      │
       │   │ Manager  │  │ Manager  │      │
       │   └──────────┘  └──────────┘      │
       │                                   │
       └───────────────┬───────────────────┘
                       │
                       ▼
               ┌──────────────┐     ┌──────────────┐
               │   Ending     │     │  SaveManager │
               │ Determiner   │     │              │
               └──────────────┘     └──────────────┘
                       │
                       ▼
               ┌──────────────┐
               │  UI View     │
               │  ViewModels  │
               └──────────────┘
```

**层级依赖规则：**
- 上层可依赖下层，下层绝不引用上层
- 同级系统通过事件通信，不直接依赖
- 所有 Manager 通过 DI 获取所需依赖

---

## 5. 二人协作分工方案

### 分工原则

1. **接口优先**：两人先共同确定所有接口和配置结构，再分头实现
2. **数据契约**：ScriptableObject 配置 + 事件结构体 = 两人之间的 API 契约
3. **依赖最小化**：每个人负责的系统之间尽量解耦，减少互相阻塞
4. **按技术侧重划分**：系统架构 vs 玩法表现

### Person A：系统与数据层 (Systems & Data)

**定位：负责所有非视觉系统、数据流、状态管理、持久化**

| 模块 | 文件 | 工作量 |
|------|------|--------|
| DI/Installer 重组 | `CoreInstaller.cs`, `SceneInstaller.cs` 重写 | 小 |
| 事件定义 | `GameEvents.cs` 所有事件结构体 | 小 |
| 配置 ScriptableObject | `ItemData.cs`, `DialogueData.cs`, `PuzzleData.cs`, `SceneData.cs`, `EndingConditionData.cs` | 中 |
| 场景流程控制器 | `SceneFlowController.cs`, `GamePhase.cs`, `SceneId.cs` | 中 |
| 道具收集系统 | `InventoryManager.cs`, `IInventoryManager.cs` | 小 |
| 存档系统 | `SaveManager.cs`, `ISaveManager.cs`, `GameSaveData.cs` | 中 |
| 音频管理器 | `AudioManager.cs`, `IAudioManager.cs`, `BGM.cs` | 中 |
| 结局判定 | `EndingDeterminer.cs`, `IEndingDeterminer.cs` | 小 |
| 生命周期管理 | 确保所有 Manager 正确注册到 LifecycleRegistry | 小 |

**技术要求：C# 架构设计、DI/事件/数据流、Unity Editor 工具**

### Person B：玩法与表现层 (Gameplay & Presentation)

**定位：负责所有玩家可见的交互、UI、场景内容、视觉反馈**

| 模块 | 文件 | 工作量 |
|------|------|--------|
| 输入系统重构 | `IPlayerInput.cs` 重写, `PlayerInputManager.cs` 重写 | 中 |
| 第一人称控制器 | `FirstPersonController.cs` (移动+视角) | 中 |
| 交互系统 | `InteractionManager.cs`, `IInteractable.cs`, `InteractableBase.cs` + 具体交互物 | 大 |
| 叙事对话系统 | `DialogueManager.cs`, `IDialogueManager.cs` | 中 |
| 解谜系统 | `PuzzleBase.cs` + 4个具体解谜实现 | 大 |
| UI 系统 | 6对 View/ViewModel + 组件 | 大 |
| 结局演出 | 结局AB的视觉呈现 | 中 |
| 场景搭建 | 7个场景的内容布置（交互物放置、动画等） | 大 |

**技术要求：Unity 场景编辑、UI系统、InputSystem、动画/Timeline**

### 接口契约（两人约定的公共 API）

这些是两人协作的"握手点"，由 Person A 先定义，Person B 基于此开发：

```csharp
// ===== Person A 提供给 Person B =====

// 事件（Person B 订阅/发布）
ItemCollectedEvent, PuzzleSolvedEvent, GamePhaseChangedEvent,
SceneReadyEvent, DialogueStartedEvent, DialogueEndedEvent, ...

// 服务接口（Person B 通过 [Inject] 获取）
IInventoryManager      // 道具查询
ISceneFlowController   // 场景切换请求
IAudioManager          // 音频播放
ISaveManager           // 存档读档
IEndingDeterminer      // 结局查询

// 配置数据（Person B 在Editor中引用 ScriptableObject）
ItemData, DialogueData, PuzzleData, SceneData
```

```csharp
// ===== Person B 提供给 Person A =====

// 交互注册（Person B 的场景物注册到 Person A 不知道的 InteractionManager）
// → 但 InteractionManager 是由 Person A 定义的接口在 DI 中注册的实现
// → Person B 实现该接口，Person A 不需要关心具体交互逻辑

// 流程推进回调（Person B 在场景/解谜完成时调用）
ISceneFlowController.AdvancePhase()
```

**关键协作点：**
- **Phase 切换触发**：Person B 的场景/解谜完成后调用 Person A 的 `SceneFlowController.AdvancePhase()`
- **存档时机通知**：Person B 的交互触发事件，Person A 的 `SaveManager` 订阅后自动存档
- **配置引用**：Person A 创建 ScriptableObject 模板，Person B 在 Editor 中实例化并填写内容

---

## 6. 开发阶段规划

### 阶段 1：基础设施搭建（共同，1-2天）

```
[共同] 确定接口设计, 走查所有接口定义
[共同] 删除旧战斗代码 (AttackConfig, BuffConfig, BuffEnums, DamageEnum, CombatController)
[Person A] 定义所有事件结构体 (GameEvents.cs)
[Person A] 创建所有 ScriptableObject 配置类
[Person A] 重写 CoreInstaller / 创建 SceneInstaller
[Person B] 创建所有 UI View/ViewModel 骨架 (空类+接口实现)
[Person B] 重构输入系统 (IPlayerInput + PlayerInputManager)
```

### 阶段 2：核心系统并行开发（2-3天）

```
[Person A]  SceneFlowController 完整实现 + 测试
[Person A]  InventoryManager 实现
[Person A]  SaveManager 实现 (PlayerPrefs JSON)
[Person A]  EndingDeterminer 实现

[Person B]  InteractionManager + InteractableBase + 基础交互物
[Person B]  FirstPersonController (移动+准星检测)
[Person B]  DialogueManager 实现
[Person B]  UI 绑定搭建 (DialogView/ViewModel, ChoiceView/ViewModel)
```

### 阶段 3：游戏系统完整实现（3-4天）

```
[Person A]  AudioManager 实现 + 音频资源导入
[Person A]  所有 ScriptableObject 配置实例创建 (在 Editor 中)
[Person A]  存档时机自动化 (订阅事件)

[Person B]  4个解谜完整实现 (PillCalendar, PhotoLight, DiaryFragment, WindChime)
[Person B]  所有场景交互物放置 + 配置关联
[Person B]  HUD, Collection, Ending UI 实现
[Person B]  场景转场效果实现
```

### 阶段 4：集成与打磨（共同，2-3天）

```
[共同] 端到端流程串联测试
[共同] 场景流程逐节点验证
[共同] 音频接入测试
[共同] 存档读档测试
[共同] 结局AB触发验证
[共同] 性能优化、Bug修复
```

---

## 附录

### A. 待删除/清理的旧文件清单

| 文件路径 | 原因 |
|----------|------|
| `Assets/Configs/AttackConfig.cs` | 战斗配置 |
| `Assets/Configs/BuffConfig.cs` | Buff配置 |
| `Assets/Common/Enums/BuffEnums.cs` | Buff枚举 |
| `Assets/Common/Enums/DamageEnum.cs` | 伤害枚举 |
| `Assets/MVVM/ViewModel/CombatController.cs` | 战斗控制器空壳 |
| `Assets/MVVM/ViewModel/GameFlowController.cs` | 空壳(将用新SceneFlowController替代) |
| `Assets/MVVM/ViewModel/HudController.cs` | 空壳(将用新HUDViewModel替代) |
| `Assets/MVVM/ViewModel/PlayerController.cs` | 已注释的旧战斗代码 |
| `Assets/MVVM/ViewModel/TestViewModel.cs` | 测试代码 |
| `Assets/MVVM/View/PlayerView.cs` | 空壳(将用新UI替代) |
| `Assets/MVVM/Model/ModelBase.cs` | 旧Model基类(不再需要) |

### B. 保留不动的核心文件

| 文件路径 | 说明 |
|----------|------|
| `Assets/Core/DI/DIContainer.cs` | 完美复用 |
| `Assets/Core/DI/Inject.cs` | 完美复用 |
| `Assets/Core/Events/EventManager.cs` | 完美复用 |
| `Assets/Core/Architecture/LifecycleRegistry.cs` | 完美复用 |
| `Assets/Core/Architecture/Interfaces/I*.cs` | 完美复用 |
| `Assets/Core/Boot/ProjectBootstrap.cs` | 完美复用 |
| `Assets/Core/Boot/ProjectContext.cs` | 微调 Installer 引用 |
| `Assets/Core/Boot/UpdateRunner.cs` | 完美复用 |
| `Assets/MVVM/Binding/*` | 完美复用 |
| `Assets/MVVM/Commands/*` | 完美复用 |
| `Assets/MVVM/ViewModel/Base/ViewModelBase.cs` | 完美复用 |
| `Assets/MVVM/ViewModel/Factory/ViewModelFactory.cs` | 完美复用 |
| `Assets/Core/Tools/MonoSingleton.cs` | 完美复用 |
