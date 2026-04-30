# GameFlow 架构文档

## 概览

GameFlow 系统采用 **MVVM + DI（依赖注入）** 架构，将剧情流程拆分为三层独立模块。Controller 负责业务逻辑，Model 持有运行时状态，View 处理表现层。模块间通过 DI 容器注入、事件驱动、以及 Unity Inspector 绑定协作。

---

## 整体架构图

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        DI Container (DIContainer)                          │
│                                                                             │
│  Register:  IGameFlowController → GameFlowController  (Singleton)          │
│             GameFlowModel       → {}                       (Singleton)      │
│             IEventCenter        → {}                       (Singleton)      │
│             IValueConverter     → {}                       (Singleton)      │
└────────────────────────────┬─────────────────────────────────────────────┘
                              │ Injection
         ┌────────────────────┼────────────────────┐
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐
│  GameFlowModel  │  │ GameFlowController│  │   SceneFlowView    │
│   (Model)       │  │   (Controller)    │  │   (View)            │
│                 │  │                  │  │                     │
│ ScriptableObject│  │ 纯 C# 类         │  │ MonoBehaviour       │
│ INotifyProperty │  │ IGameFlowController│  │ StrictLifecycleMono│
│ Changed         │  │ IInitializable    │  │                     │
│                 │  │ IDisposable       │  │                     │
└────────┬────────┘  └───────┬──────────┘  └──────────┬──────────┘
         │                    │                        │
         │   写入控制          │       事件订阅           │
         │◄───────────────────┤    OnPhaseChanged        │
         │                    ├────────────────────────►│
         │                    │    OnPhaseComplete       │
         │                    ├────────────────────────►│
         │                    │                         │
         │                    │  ConfirmTransition()     │
         │                    │◄────────────────────────┤
         │                    │                         │
         │              只读绑定                         │
         │◄══ PropertyBinding ═══════════════════════════│
         │◄══ CommandBinding  ═══════════════════════════│
         │                                               │
         ▼                                               ▼
┌─────────────────┐                           ┌─────────────────────┐
│   Unity UI      │                           │   Game Events       │
│ (Button/Slider/ │                           │ (ItemCollectedEvent │
│  Text/... )     │                           │  PuzzleSolvedEvent  │
│                 │                           │  InteractionEvent   │
│ 绑定到 Model 属性│                           │  TriggerEnterEvent) │
│ PhaseProgress   │                           │                     │
│ CurrentPhase    │                           └─────────────────────┘
└─────────────────┘
```

---

## 类关系 UML

```
┌─────────────────────────────────┐
│     «interface»                  │
│  IGameFlowController             │
│  (IInitializable, IDisposable)   │
├─────────────────────────────────┤
│ + CurrentPhase: GamePhase        │
│ + GameData: GameSaveDataRuntime  │
│ + OnPhaseChanged: event          │
│ + OnPhaseComplete: event         │
│ + ConfirmTransition(phase)       │
└──────────────┬──────────────────┘
               │ implements
               ▼
┌─────────────────────────────────────────────────────────────┐
│               GameFlowController                             │
│               (纯业务逻辑，不依赖 UnityEngine)                  │
├─────────────────────────────────────────────────────────────┤
│ - _events: IEventCenter              [Inject]               │
│ - _model: GameFlowModel              [Inject]               │
│ - _configs: Dictionary<GamePhase, GamePhaseConfig>           │
│ - _currentConfig: GamePhaseConfig                            │
│ - _completedBeats: HashSet<string>                           │
├─────────────────────────────────────────────────────────────┤
│ + Initialize(): void                                         │
│ + Dispose(): void                                            │
│ + ConfirmTransition(GamePhase): void                         │
│ + GetSaveState(): GameSaveDataRuntime                        │
│ - TryCompleteBeat(StoryBeatType, string): void               │
│ - DetermineNextPhase(): GamePhase                            │
│ - StartPhase(GamePhase): void                                │
│ - LoadSaveData(): Task                                       │
│ - LoadPhaseConfigs(): Task                                   │
│ - SubscribeEvents(): void                                    │
└──────────────┬──────────────────────────────┬───────────────┘
               │ writes                        │ subscribes
               ▼                               ▼
┌──────────────────────────────┐  ┌──────────────────────────┐
│        GameFlowModel          │  │   «interface»             │
│        (ScriptableObject)     │  │   IEventCenter            │
│      INotifyPropertyChanged   │  │                          │
├──────────────────────────────┤  │ + Subscribe<T>(Action<T>) │
│ + CurrentPhase: GamePhase     │  │ + Publish<T>(T)           │
│ + IsTransitioning: bool       │  └──────────────────────────┘
│ + CompletedBeatCount: int     │
│ + TotalBeatCount: int         │
│ + PhaseProgress: float (calc) │
├──────────────────────────────┤
│ + ApplyBeatProgress(int,int)  │
│ + ApplyPhase(GamePhase,int,int)│
│ + SetTransitioning(bool)      │
│ - PropertyChanged: event      │
└───────┬──────────────────────┘
        │ binds via PropertyBinding / CommandBinding
        ▼
┌──────────────────────────────┐
│          Unity UI             │
│ (Button, Slider, Text, ...)  │
└──────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│              SceneFlowView                                    │
│              (MonoBehaviour, StrictLifecycle)                  │
├──────────────────────────────────────────────────────────────┤
│ - _controller: IGameFlowController      [Inject]              │
├──────────────────────────────────────────────────────────────┤
│ + OnStartExternal(): void                                     │
│ + OnShutdown(): void                                          │
│ - HandlePhaseComplete(GamePhase): async void                  │
│ - HandlePhaseChanged(GamePhase): async void                   │
│ - LoadPhaseConfig(GamePhase): Task<GamePhaseConfig>           │
└──────────────────────────────────────────────────────────────┘
        │ depends on
        ▼
┌──────────────────────────────────┐
│        GamePhaseConfig            │
│        (ScriptableObject)         │
├──────────────────────────────────┤
│ + PhaseId: GamePhase              │
│ + DisplayName: string             │
│ + SceneAssetPath: string          │
│ + EntryDialogueId: string         │
│ + ExitDialogueId: string          │
│ + RequiredBeats: List<StoryBeat>  │
│ + DefaultNextPhase: GamePhase     │
│ + AltNextPhase: GamePhase         │
│ + IsEndingBranch: bool            │
│ + TransitionSFX: string           │
│ + TransitionDuration: float       │
└──────────────────────────────────┘
        │ contains
        ▼
┌──────────────────────────────────┐
│          StoryBeat                │
│          [Serializable]           │
├──────────────────────────────────┤
│ + BeatId: string                 │
│ + Type: StoryBeatType            │
│ + TargetId: string               │
│ + Description: string            │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│    «enum» GamePhase               │
├──────────────────────────────────┤
│ None                              │
│ Phase1_SurfaceLivingRoom_Initial  │
│ Phase1_SurfaceLivingRoom_Investigate│
│ Phase2_InnerPark                  │
│ Phase3_SurfaceLivingRoom_Photo    │
│ Phase4_InnerBedroom               │
│ Phase5_InnerBalcony               │
│ Phase6_InnerLivingRoom_Mirror     │
│ Phase7_Epilogue_A                 │
│ Phase7_Epilogue_B                 │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│    «enum» StoryBeatType           │
├──────────────────────────────────┤
│ CollectItem                       │
│ SolvePuzzle                       │
│ CompleteDialogue                  │
│ InteractWithObject                │
│ EnterTrigger                      │
└──────────────────────────────────┘


┌──────────────────────────────────┐
│        GameSaveData               │
│        (ScriptableObject)         │
├──────────────────────────────────┤
│ + currentPhase: GamePhase         │
│ + currentSceneId: string          │
│ + completedBeatIds: List<string>  │
│ + collectedItemIds: List<string>  │
│ + solvedPuzzleIds: List<string>   │
│ + completedDialogueIds: List<string>│
│ + playTimeSeconds: float          │
│ + saveDateTime: string            │
└──────────────┬───────────────────┘
               │ wraps
               ▼
┌──────────────────────────────────┐
│    GameSaveDataRuntime            │
├──────────────────────────────────┤
│ + CurrentPhase: GamePhase         │
│ + CurrentSceneId: string          │
│ + CompletedBeatIds: HashSet       │
│ + CollectedItemIds: HashSet       │
│ + SolvedPuzzleIds: HashSet        │
│ + CompletedDialogueIds: HashSet   │
└──────────────────────────────────┘


┌─────────────────────────────────────────────────────────┐
│              MVVM Binding Layer                          │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌───────────────────────┐  ┌───────────────────────┐   │
│  │    PropertyBinding     │  │    CommandBinding      │   │
│  │    (IPropertyBinding)  │  │    (ICommandBinding)   │   │
│  ├───────────────────────┤  ├───────────────────────┤   │
│  │ - _source: Object      │  │ - _source: Object      │   │
│  │   (支持 SO / MB)       │  │   (支持 SO / MB)       │   │
│  │ - _targetComponent     │  │ - _targetComponent     │   │
│  │ - _mode: BindingMode   │  │ - _commandName         │   │
│  │ - _sourceProperty      │  │ - _eventName           │   │
│  │ - _targetProperty      │  │ - _parameterType       │   │
│  ├───────────────────────┤  ├───────────────────────┤   │
│  │ + Bind()               │  │ + Bind()               │   │
│  │ + UnBind()             │  │ + UnBind()             │   │
│  │ + UpdateSource()       │  │ + UpdateSource()       │   │
│  │ + UpdateTarget()       │  │ + UpdateTarget()       │   │
│  └───────────────────────┘  └───────────────────────┘   │
│                                                          │
│  BindingMode:  OneWay / TwoWay / OneWayToSource           │
│  BindingParameterType: None / FixedValue / PropertyPath  │
│                       / EventArgument                     │
└─────────────────────────────────────────────────────────┘
```

---

## 数据流

### Beat 完成流程（事件驱动）

```
                    ┌──────────────────────┐
                    │  Game Event (trigger) │
                    │  ItemCollectedEvent   │
                    │  PuzzleSolvedEvent    │
                    │  InteractionPerformed │
                    │  TriggerEnterEvent    │
                    └──────────┬───────────┘
                               │ _events.Publish()
                               ▼
                    ┌──────────────────────┐
                    │ GameFlowController   │
                    │ TryCompleteBeat()    │
                    │   → Match beat       │
                    │   → Add to HashSet   │
                    └──────────┬───────────┘
                               │
                    ┌──────────┴───────────┐
                    │                      │
                    ▼                      ▼
          ┌──────────────────┐   ┌──────────────────┐
          │ GameFlowModel     │   │  AllBeatsDone()?  │
          │ ApplyBeatProgress │   │  → Yes           │
          └────────┬─────────┘   └────────┬─────────┘
                   │                      │
                   ▼                      ▼
          ┌──────────────────┐   ┌──────────────────────────┐
          │ INotifyProperty  │   │ OnPhaseComplete?.Invoke() │
          │ Changed → UI 更新│   │ (携带 nextPhase)          │
          └──────────────────┘   └────────────┬─────────────┘
                                              │
                                              ▼
                                     ┌──────────────────────┐
                                     │  SceneFlowView       │
                                     │  HandlePhaseComplete │
                                     │  1. 离开对话          │
                                     │  2. 黑屏淡入          │
                                     │  3. 加载场景          │
                                     │  4. ConfirmTransition │
                                     └──────────┬───────────┘
                                                │
                                                ▼
                                     ┌──────────────────────┐
                                     │ GameFlowController   │
                                     │ ConfirmTransition()  │
                                     │  → 更新 _currentConfig │
                                     │  → Model.ApplyPhase()│
                                     │  → OnPhaseChanged    │
                                     └──────────────────────┘
```

### 阶段切换流程（时序图）

```
     Player/Event      GameFlowController       GameFlowModel        SceneFlowView
         │                    │                      │                    │
         │  Trigger Event     │                      │                    │
         │───────────────────►│                      │                    │
         │                    │                      │                    │
         │                    │  TryCompleteBeat()   │                    │
         │                    │─────────────────────►│                    │
         │                    │  ApplyBeatProgress() │                    │
         │                    │                      │                    │
         │                    │  [AllBeatsDone?]     │                    │
         │                    │  DetermineNextPhase()│                    │
         │                    │                      │                    │
         │                    │  OnPhaseComplete     │                    │
         │                    │─────────────────────────────────────────►│
         │                    │                      │                    │
         │                    │                      │    HandlePhaseComplete
         │                    │                      │    ┌──────────────┐
         │                    │                      │    │ 播离开对话     │
         │                    │                      │    │ 黑屏淡入       │
         │                    │                      │    │ 加载新场景     │
         │                    │                      │    └──────────────┘
         │                    │                      │                    │
         │                    │  ConfirmTransition() │                    │
         │                    │◄─────────────────────────────────────────┤
         │                    │                      │                    │
         │                    │  ApplyPhase()        │                    │
         │                    │─────────────────────►│                    │
         │                    │                      │                    │
         │                    │  OnPhaseChanged      │                    │
         │                    │──────────────────────────────────────────►│
         │                    │                      │                    │
         │                    │                      │    HandlePhaseChanged
         │                    │                      │    ┌──────────────┐
         │                    │                      │    │ 切换BGM       │
         │                    │                      │    │ 黑屏淡出     │
         │                    │                      │    │ 播进入对话    │
         │                    │                      │    └──────────────┘
         │                    │                      │                    │
```

---

## DI 注册关系（GamePlayInstaller）

```
DIContainer
├── IGameFlowController → GameFlowController       (Singleton)
├── GameFlowModel       → Addressables load         (Singleton)
│     ↑ 加载路径: "Configs/GameFlowModel.asset"
├── IEventCenter        → (由 Core 层注册)
└── IValueConverter     → (由 Core 层注册)
```

---

## MVVM Binding 映射

GameFlow 使用 Unity Inspector 直接绑定 Model → UI，无需 View 层代码介入：

### PropertyBinding（数据 → UI）

| 绑定源 (`_source`) | 源属性 | 目标组件 | 目标属性 | 方向 |
|---|---|---|---|---|
| GameFlowModel.asset | `PhaseProgress` | Slider | `value` | OneWay |
| GameFlowModel.asset | `CurrentPhase` | Text | `text` | OneWay |
| GameFlowModel.asset | `IsTransitioning` | CanvasGroup | `interactable` | OneWay(反) |

### CommandBinding（UI 事件 → 命令）

| 绑定源 (`_source`) | 命令名 | 目标组件 | 事件 | 参数 |
|---|---|---|---|---|
| GameFlowModel.asset (或其他 VM) | `SaveCommand` | Button | `onClick` | None |

---

## 关键设计决策

### 1. Controller 不碰 View

`GameFlowController` 是纯 C# 类，仅依赖 `IEventCenter` 和 `GameFlowModel`。它通过 `OnPhaseChanged` / `OnPhaseComplete` 事件通知 View，不直接调用任何 Unity 场景操作。

### 2. ScriptableObject 作为 Model + Binding 源

`GameFlowModel` 实现了 `INotifyPropertyChanged`，可直接挂到 `PropertyBinding._source` 和 `CommandBinding._source`。这意味着：
- **UI 绑定在 Inspector 中配置**，无需手写 `GetComponent<>()` / `Find()` 等同步代码
- **Model 状态变更自动推送 UI 更新**（通过 `OnChanged()` → `PropertyChanged` → `PropertyBinding.UpdateTarget()`）
- **Binding 支持 SO 和 MB 两种数据源**（`_source` 类型从 `MonoBehaviour` 改为 `Object`）

### 3. 事件驱动的 Beat 系统

游戏内交互（收集道具、解谜、进入触发区）通过 `IEventCenter` 发布事件，Controller 订阅后将事件匹配到对应 `StoryBeat`，完成进度判定。这使得：
- 互动模块与剧情模块完全解耦
- 新增 Beat 类型只需新增事件枚举和 `StoryBeatType` 条目

### 4. 存档通过 Addressables 加载

`GameSaveData`（SO 资产）和 `GamePhaseConfig`（多个 SO 资产）都通过 Addressables 异步加载。`GameSaveDataRuntime` 是存档数据的运行时封装，使用 `HashSet` 替代 `List` 以提高查找效率。

---

## 模块依赖图（简洁版）

```
        ┌─────────────────┐
        │ Core.DI          │ ← 提供 DIContainer, [Inject]
        │ Core.Architecture│ ← 提供 InstallerAsset,
        │                   │    StrictLifecycleMonoBehaviour,
        │                   │    IInitializable, IDisposable
        │ Core.Events       │ ← 提供 IEventCenter, 事件基类
        └────────┬──────────┘
                 │
    ┌────────────┼────────────┐
    │            │            │
    ▼            ▼            ▼
┌────────┐ ┌──────────┐ ┌──────────────┐
│GameFlow│ │SceneFlow │ │MVVM.Binding  │
│Model   │ │View      │ │              │
│(SO)    │ │(MB)      │ │PropertyBinding│
│        │ │          │ │CommandBinding │
│        │ │          │ │(MB)          │
└───┬────┘ └────┬─────┘ └──────┬───────┘
    │           │              │
    │    ┌──────┘              │
    │    │                     │
    ▼    ▼                     │
┌─────────────┐                │
│GameFlow     │                │
│Controller   │                │
│(pure C#)    │                │
└──────┬──────┘                │
       │                       │
       ▼                       ▼
┌────────────────────────────────┐
│ GamePhaseConfig (SO)           │
│ GameSaveData (SO)              │
│ StoryBeat (Serializable)       │
└────────────────────────────────┘
```

```
依赖方向：上层 → 下层（箭头指向被依赖方）

Unity.Addressables ──► GamePlayInstaller ──► DIContainer
                                              │
                              ┌───────────────┼───────────────┐
                              │               │               │
                              ▼               ▼               ▼
                     GameFlowController   SceneFlowView   Bindings
                              │               │               │
                              ▼               ▼               │
                     GameFlowModel     GamePhaseConfig ←──────┘
                         (SO)             (SO)
```

---

## 文件清单

| 文件 | 角色 | 命名空间 |
|---|---|---|
| `Assets/Gameplay/Interfaces/IGameFlowController.cs` | Controller 接口 | `Gameplay.Interfaces` |
| `Assets/Gameplay/SceneFlow/GameFlowController.cs` | Controller 实现 | `Gameplay.SceneFlow` |
| `Assets/Gameplay/SceneFlow/SceneFlowView.cs` | View 表现层 | `Gameplay.SceneFlow` |
| `Assets/Gameplay/SO/GameFlowModel.cs` | Model 数据层 | `Gameplay.SO` |
| `Assets/Gameplay/SO/GamePhaseConfig.cs` | 阶段配置 SO | `Gameplay.SO` |
| `Assets/Gameplay/SO/GameSaveData.cs` | 存档 SO + Runtime | `Gameplay.SO` |
| `Assets/Gameplay/SceneFlow/GamePhase.cs` | 阶段枚举 | `Gameplay.SceneFlow` |
| `Assets/Gameplay/SceneFlow/StoryBeat.cs` | Beat 定义 + 枚举 | `Gameplay.SceneFlow` |
| `Assets/Gameplay/Installer/GamePlayInstaller.cs` | DI 注册 | `Gameplay.Installer` |
| `Assets/MVVM/Binding/PropertyBinding.cs` | 属性绑定 | `MVVM.Binding` |
| `Assets/MVVM/Binding/CommandBinding.cs` | 命令绑定 | `MVVM.Binding` |
