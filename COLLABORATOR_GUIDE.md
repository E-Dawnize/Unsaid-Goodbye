# Unsaid Goodbye — 协作者上手指南

> 本文档基于 **2026-05-05 实际代码状态**，描述已实现的框架、游戏全流程、外部接口和待完成模块。
> 另有一份早期设计文档 `ARCHITECTURE.md`（2026-04-29），部分内容与当前实现有出入，以本文档为准。
>
> **近期更新**（2026-05-05）：
> - **DI 容器重构**：单例存储 key 从 `int`(descriptor ID) 改为 `Type`(具体实现类型)，同一实例注册为多个接口时正确命中同一槽位。`Register*<T,Impl>()` 新增具体类型校验，防止接口/抽象类作为实现类型。`ResolveScope` 修复父 Scope 查找 bug
> - 修复启动时序：`SetupSceneScoping` 移到 `ExecuteLifecycle` 之前，Scoped ViewModel 可在初始化阶段正确注入
> - 修复 `LifecycleRegistry` 注入失败重试机制
> - 新建 `MainMenuView.cs`，UI 绑定从 Inspector 反射改为代码直连（MVVM View 模式）
> - `StartSceneInstaller` 已添加到 `BootConfig.sceneInstallers`
> - 场景加载 API 从 `SceneManager` 切换为 `Addressables.LoadSceneAsync`

---

## 目录

1. [快速概览：项目是什么](#1-快速概览项目是什么)
2. [架构分层与关键文件](#2-架构分层与关键文件)
3. [启动全流程（从引擎启动到游戏就绪）](#3-启动全流程从引擎启动到游戏就绪)
4. [游戏主循环：阶段→节拍→转场→存档](#4-游戏主循环阶段节拍转场存档)
5. [外部接口手册（如何接入新功能）](#5-外部接口手册如何接入新功能)
6. [ScriptableObject 配置体系](#6-scriptableobject-配置体系)
7. [当前场景与场景 Installer 机制](#7-当前场景与场景-installer-机制)
8. [已实现 vs 未实现（完整清单）](#8-已实现-vs-未实现完整清单)
9. [推进优先级建议](#9-推进优先级建议)

---

## 1. 快速概览：项目是什么

**Unsaid Goodbye** — 2D 叙事解谜游戏。玩家通过鼠标点击探索场景、收集道具、完成解谜来推进剧情。故事按"阶段（Phase）"线性推进，每个阶段有一组必须完成的"节拍（StoryBeat）"，全部完成后自动切换到下一阶段。存在多结局分支。

**技术栈**：Unity 2022+ / Addressables 资源管理 / Unity InputSystem / 自研轻量 DI 容器 + MVVM 绑定

**已跑通的核心链路**：
```
引擎启动 → DI容器初始化 → 全局+场景服务注册 → 时序保证的注入
→ 生命周期执行（Initialize → Start）→ GameFlowManager就绪
→ 玩家点击场景物体 → 事件发布 → Beat匹配
→ 阶段推进 → Addressables场景加载 → 存档
```

---

## 2. 架构分层与关键文件

```
┌──────────────────────────────────────────┐
│  Scenes: Start / OutterWorld /           │
│          InnerWorld_Park                  │
├──────────────────────────────────────────┤
│  Gameplay: GameFlowManager /          │
│            GameFlowView /               │
│            InteractableObject /          │
│            SaveManager / SO Configs      │
├──────────────────────────────────────────┤
│  MVVM: ViewModelBase / PropertyBinding / │
│        CommandBinding / Commands          │
├──────────────────────────────────────────┤
│  Core: DIContainer / LifecycleRegistry / │
│        EventCenter / Installer System /   │
│        ScopeProvider / UpdateRunner       │
├──────────────────────────────────────────┤
│  Unity: InputSystem / Addressables        │
└──────────────────────────────────────────┘
```

### Core 层关键文件

| 文件 | 一句话职责 |
|------|-----------|
| `Core/DI/DIContainer.cs` | 自研 IoC 容器：Singleton/Scoped/Transient，构造器+字段+属性注入，循环依赖检测，启动时依赖图验证。**单例 key = 具体实现 Type**（非 descriptor ID），同一实例注册为多接口时命中同一槽位 |
| `Core/DI/Inject.cs` | `[Inject]` / `[InjectOptional]` 标记 + DIContainer.Inject() 反射注入逻辑 |
| `Core/Architecture/LifecycleRegistry.cs` | 静态全局注册表，保证 所有 `IInitializable.Initialize()` 完成后才执行 `IStartable.OnStart()`。注入失败自动加入重试队列，在 `ProcessDelayedInjection()` 中重新尝试 |
| `Core/Architecture/LifecycleMonoBehaviour.cs` | **继承此类而非普通 MonoBehaviour**：封存 Awake/Start，子类重写 `OnInitialize()` / `OnStartExternal()` / `OnShutdown()` |
| `Core/Architecture/InstallerAsset.cs` | Installer 基类（ScriptableObject），重写 `Register(DIContainer)` 注册服务 |
| `Core/Architecture/InstallerConfig.cs` | 总配置 SO：`globalInstallers` 列表 + `sceneInstallers` 列表，按 order 排序执行 |
| `Core/Events/EventManager.cs` | 事件总线：`Subscribe<T>` / `Unsubscribe<T>` / `Publish<T>`，T 必须是 struct |
| `Core/Events/EventDefinitions/EventStructs.cs` | 已定义的事件：ItemCollected / PuzzleSolved / DialogueEnded / InteractionPerformed / TriggerEnter / StoryBeatCompleted |
| `Core/Boot/ProjectBootstrap.cs` | `[RuntimeInitializeOnLoadMethod]` 钩子：BeforeSceneLoad → ProjectContext.Ensure() |
| `Core/Boot/ProjectContext.cs` | 启动主流程：创建DI容器→加载InstallerConfig→验证依赖→**SetupSceneScoping（在生命周期之前）**→执行生命周期→StartGameLoop |
| `Core/Boot/SceneScopeRunner.cs` | 监听 `SceneManager.sceneLoaded/Unloaded`，自动创建/销毁场景级 Scope |
| `Core/Boot/UpdateRunner.cs` | 驱动所有 `ITickable` 的 MonoBehaviour Update |
| `Core/Architecture/ScopeProvider.cs` | 全局单例，追踪"当前活跃场景"的 DI Scope |

### MVVM 层关键文件

| 文件 | 一句话职责 |
|------|-----------|
| `MVVM/ViewModel/Base/ViewModelBase.cs` | ViewModel 基类：`INotifyPropertyChanged` + `SetProperty<T>()` + 内建 Command 工厂方法 |
| `MVVM/View/MainMenuView.cs` | **主菜单 View（新建）**：继承 `StrictLifecycleMonoBehaviour`，`[Inject]` 注入 ViewModel，`OnStartExternal` 中代码绑定 UI 事件，替代 Inspector 字符串反射方式 |
| `MVVM/ViewModel/MainMenuViewModel.cs` | 主菜单 ViewModel：`StartGameCommand`（AsyncCommand）+ `Volume` 属性，通过 `Addressables.LoadSceneAsync` 加载游戏场景 |
| `MVVM/Binding/PropertyBinding.cs` | 旧方式：挂 GameObject 上，Inspector 填字符串反射绑定（**新 UI 开发不再使用，改用 View 模式**） |
| `MVVM/Binding/CommandBinding.cs` | 旧方式：挂 GameObject 上，Inspector 填字符串反射绑定（**新 UI 开发不再使用，改用 View 模式**） |
| `MVVM/Commands/RelayCommand.cs` | 同步 ICommand（`RelayCommand` / `RelayCommand<T>`） |
| `MVVM/Commands/AsyncCommand.cs` | 异步 ICommand（`AsyncCommand` / `AsyncCommand<T>`），执行中自动禁用按钮 |
| `MVVM/ViewModel/Factory/ViewModelFactory.cs` | 通过 DI 容器创建 Scoped/Transient/Singleton ViewModel |
| `MVVM/Binding/BindingManager.cs` | 绑定管理器（完整实现，但 View 模式直接管理绑定，此管理器暂未接入） |

### Gameplay 层关键文件

| 文件 | 一句话职责 |
|------|-----------|
| `Gameplay/SceneFlow/GameFlowManager.cs` | **核心剧情控制器**：订阅事件→Beat匹配→阶段推进→触发转场 |
| `Gameplay/SceneFlow/GameFlowView.cs` | **表现层**：监听 Controller 事件，执行转场动画、`Addressables.LoadSceneAsync` 场景加载、TODO(BGM/对话) |
| `Gameplay/SceneFlow/StoryBeat.cs` | SO：一个节拍 = Type(收集/解谜/对话/交互/触发) + TargetId + Description |
| `Gameplay/SceneFlow/GamePhase.cs` | 枚举：Phase1~Phase7 + Epilogue A/B，共 10 个阶段 |
| `Gameplay/SO/GamePhaseConfig.cs` | SO：阶段配置 = 场景路径 + RequiredBeats 列表 + 下一阶段 + 转场参数 |
| `Gameplay/SO/GameFlowModel.cs` | MVVM Model：`INotifyPropertyChanged`，暴露 PhaseProgress(0~1) 供 UI 绑定 |
| `Gameplay/SO/GameSaveData.cs` | 运行时存档数据（HashSet），与 GameSaveDto 互转 |
| `Gameplay/Save/SaveManager.cs` | JSON 文件读写：`Application.persistentDataPath/save.json` |
| `Gameplay/Save/GameSaveDto.cs` | [Serializable] 存档 DTO |
| `Gameplay/Interactions/InteractableObject.cs` | 挂场景物体上：OnMouseDown(点击)/OnTriggerEnter2D(触发区) → 发布事件到 EventCenter |
| `Gameplay/Installer/GamePlayInstaller.cs` | 注册 IGameFlowManager / ISaveManager / GameFlowModel |
| `Gameplay/Installer/StartSceneInstaller.cs` | 注册 MainMenuViewModel (Scoped) |

---

## 3. 启动全流程（从引擎启动到游戏就绪）

```
Unity 引擎启动
│
├─ [BeforeSceneLoad] ProjectBootstrap.Boot()
│      └─ ProjectContext.Ensure()
│            ├─ new GameObject("ProjectContext") → DontDestroyOnLoad
│            └─ AddComponent<UpdateRunner>()
│
└─ ProjectContext.Boot()  [async]
      │
      ├── 1. CreateDIContainer()
      │      new DIContainer() → CreateScope() → _projectScope
      │
      ├── 2. RegisterInstallers()  [await Addressables.LoadAssetAsync<InstallerConfig>("BootConfig")]
      │      加载 BootConfig.asset → 遍历 GlobalInstallersSorted (按order排序):
      │        ├─ CoreInstaller        → EventManager, PlayerInput, ViewModelFactory, ScopeProvider
      │        └─ GamePlayInstaller     → GameFlowManager, PlayerManager, SaveManager, GameFlowModel
      │      此时场景加载，GameObject Awake() 触发 [Inject]，
      │      若依赖的 Scoped 服务未注册 → 加入 _pendingInjection 重试队列
      │
      ├── 3. ValidateDependencies()
      │      遍历所有 Singleton 注册 → 逐一尝试解析 → 报告循环依赖/缺失
      │
      ├── 4. SetupLifecycleRegistry()
      │      LifecycleRegistry.SetContainer(container, _projectScope)
      │
      ├── 5. SetupSceneScoping()        ← 在生命周期之前执行！
      │      ├─ 为初始场景预创建 Scope
      │      ├─ 注入 sceneInstallers → StartSceneInstaller 注册 MainMenuViewModel (Scoped)
      │      ├─ 初始化 Scoped 服务的 IInitializable + IStartable
      │      └─ SceneScopeRunner.Attach() → 监听后续场景加载/卸载
      │
      ├── 6. ExecuteLifecycle()
      │      ├─ PreRegisterLifecycleServices()
      │      │    ResolveAll<IInitializable>() → 触发 DI 构造 → 自动注册到 LifecycleRegistry
      │      │    ResolveAll<IStartable>()
      │      ├─ ProcessDelayedInjection()     ← 重试 Awake 时失败的 [Inject]
      │      ├─ LifecycleRegistry.InitializeAll()  ← 所有 IInitializable.Initialize()
      │      │    ├─ GameFlowManager.Initialize()
      │      │    │    ├─ LoadSaveData() → SaveManager
      │      │    │    ├─ await LoadPhaseConfigs() → Addressables (label="GamePhaseConfig")
      │      │    │    ├─ SubscribeEvents() → 5种事件
      │      │    │    └─ StartPhase(Phase1) 或 RestoreFromSave()
      │      │    └─ EventManager.Initialize()
      │      └─ LifecycleRegistry.StartAll()    ← 所有 IStartable.OnStart()
      │           └─ MainMenuView.OnStartExternal() → 代码绑定 UI 事件
      │
      └── 7. StartGameLoop()
             ResolveAll<ITickable>() → UpdateRunner.Register()
```

**关键点**：启动完成后，DI 容器已就绪、所有服务已初始化（包括 Scoped ViewModel）、GameFlowManager 处于 Phase1、等待玩家交互。

**时序保证**：`SetupSceneScoping()` 在 `ExecuteLifecycle()` 之前执行，确保场景 Installer 注册的 Scoped 服务在 `[Inject]` 和 `OnInitialize` 阶段可用。若 Awake 时注入失败（Scoped 服务尚未注册），`LifecycleRegistry` 会将组件加入 `_pendingInjection` 队列，在 `ProcessDelayedInjection()` 中重试。

---

## 4. 游戏主循环：阶段→节拍→转场→存档

### 4.1 核心数据模型

- **GamePhase**（枚举）：10 个阶段
- **StoryBeat**（SO）：`{ BeatId, StoryBeatType, TargetId, Description }`
- **GamePhaseConfig**（SO）：`{ PhaseId, SceneAssetPath, RequiredBeats[], DefaultNextPhase, AltNextPhase, IsEndingBranch, TransitionDuration }`
- **GameFlowModel**（纯 C#，INotifyPropertyChanged）：`{ CurrentPhase, IsTransitioning, CompletedBeatCount, TotalBeatCount, PhaseProgress(0~1) }`

### 4.2 StoryBeatType 枚举

```csharp
CollectItem          // → ItemCollectedEvent
SolvePuzzle          // → PuzzleSolvedEvent
CompleteDialogue     // → DialogueEndedEvent
InteractWithObject   // → InteractionPerformedEvent
EnterTrigger         // → TriggerEnterEvent
```

### 4.3 玩家交互 → 流程推进完整链路

```
1. 玩家点击场景物体 / 进入 Trigger 区域
      │
2. InteractableObject.Fire()
      │  根据 InteractableType 发布对应 struct 事件:
      │    Collectible   → new ItemCollectedEvent { ItemID = _targetId }
      │    Puzzle        → new PuzzleSolvedEvent { PuzzleID = _targetId }
      │    Interactive   → new InteractionPerformedEvent { InteractableID = _targetId }
      │    TriggerZone   → new TriggerEnterEvent { TriggerID = _targetId }
      │
3. GameFlowManager.TryCompleteBeat(beatType, targetId)
      │  遍历 _currentConfig.RequiredBeats，匹配 Type + TargetId
      │  匹配成功 → _completedBeats.Add(beatId)
      │           → _model.ApplyBeatProgress()  — UI 进度条自动更新
      │           → Publish(StoryBeatCompletedEvent)
      │           → 检查 AllBeatsDone()?
      │
4. AllBeatsDone() == true
      │  DetermineNextPhase():
      │    普通阶段 → DefaultNextPhase
      │    结局分支 → TODO: IEndingDeterminer (目前返回 DefaultNextPhase)
      │  OnPhaseComplete?.Invoke(nextPhase)
      │
5. GameFlowView.HandlePhaseComplete(nextPhase)  [async]
      │  ├─ 播放离开对话  (TODO: IDialogueManager)
      │  ├─ 黑屏淡入      (TODO: FadeToBlack)
      │  ├─ SceneManager.LoadSceneAsync(config.SceneAssetPath)
      │  └─ _controller.ConfirmTransition(nextPhase)
      │
6. GameFlowManager.ConfirmTransition(newPhase)
      │  切换 _currentConfig，清空 _completedBeats
      │  _model.ApplyPhase(newPhase)  — UI 进度条重置
      │  OnPhaseChanged?.Invoke(newPhase)
      │
7. GameFlowView.HandlePhaseChanged(newPhase)  [async]
      │  ├─ 切换 BGM       (TODO: IAudioManager)
      │  ├─ 黑屏淡出       (TODO: FadeFromBlack)
      │  └─ 播放进入对话    (TODO: IDialogueManager)

8. 循环至步骤 1
```

### 4.4 存档机制

- **文件位置**：`Application.persistentDataPath/save.json`
- **格式**：JSON（Unity JsonUtility 序列化 GameSaveDto）
- **保存内容**：currentPhase / currentSceneId / completedBeatIds / collectedItemIds / solvedPuzzleIds / completedDialogueIds / playTimeSeconds
- **触发时机**：`GameFlowManager.GetSaveState()` 由外部调用（目前未自动触发，需在主菜单/暂停菜单中调用）

---

## 5. 外部接口手册（如何接入新功能）

### 5.1 生命周期接口

```csharp
// 这三个接口是所有服务的生命周期入口
public interface IInitializable { void Initialize(); }  // DI注入完成后，所有组件的Initialize全部完成才进入OnStart
public interface IStartable     { void OnStart(); }     // 所有Initialize完成后，可以安全与其他组件交互
public interface ITickable     { void Tick(float dt); } // 每帧调用，由UpdateRunner驱动
```

**使用方式 A：纯 C# 服务**
```csharp
public class MyService : IInitializable, IDisposable
{
    [Inject] private IEventCenter _events;

    public void Initialize()
    {
        _events.Subscribe<SomeEvent>(OnSomeEvent);
    }

    public void Dispose()
    {
        _events.Unsubscribe<SomeEvent>(OnSomeEvent);
    }
}

// 在 Installer 中注册
container.RegisterSingleton<MyService, MyService>();
```

**使用方式 B：MonoBehaviour（挂场景 GameObject）**
```csharp
public class MySceneComponent : StrictLifecycleMonoBehaviour
{
    [Inject] private IEventCenter _events;

    protected override void OnInitialize()
    {
        // 依赖已注入，可以安全访问 _events
    }

    protected override void OnStartExternal()
    {
        // 所有组件的 Initialize 都已完成，可以安全交互
    }

    protected override void OnShutdown()
    {
        // 清理
    }
}
```

### 5.2 DI 注入标记

```csharp
[Inject] private ISomeService _service;               // 必须存在，否则抛异常
[InjectOptional] private ISomeOptionalService _opt;    // 可选，缺失不报错
```

DI 容器自动选择构造函数（优先 `[Inject]` 标记的，否则选参数最多的），构造函数参数 + 标记了 `[Inject]` 的字段/属性都会被注入。

**单例存储设计**：容器用 `ConcurrentDictionary<Type, object>` 存储单例，key = 具体实现类型（`typeof(ConcreteClass)`），而非 descriptor ID。这意味着同一个实例通过不同接口注册时（如 `RegisterSingleton<DIContainer>(container)` + `RegisterSingleton<IServiceProvider>(container)`），两个描述符的 `ImplementationType` 都是 `typeof(DIContainer)`，解析任一接口都命中同一槽位。

**解析路径**：
- `ImplementationType != null`（类型注册 / 实例注册）→ `_singletonInstances[ImplementationType]`
- `ImplementationType == null`（纯工厂注册）→ `_singletonFactoryResults[descriptor.Id]`（ID 备用）

`RegisterSingleton<T, Impl>()` 会校验 `typeof(Impl)` 必须是具体类（非接口/抽象类）。Scoped 同理，`Scope.ScopedInstances` 也是 `ConcurrentDictionary<Type, object>`。

### 5.3 安装器系统

```csharp
// 1. 创建 Installer
[CreateAssetMenu(fileName = "MyInstaller", menuName = "Boot/MyInstaller")]
public class MyInstaller : InstallerAsset
{
    public override void Register(DIContainer container)
    {
        // 全局单例
        container.RegisterSingleton<IMyService, MyService>();

        // 随场景释放
        container.RegisterScoped<MyViewModel, MyViewModel>();

        // 每次解析都创建新实例
        container.RegisterTransient<IMyHelper, MyHelper>();

        // 工厂注册
        container.RegisterSingleton<IMyService>(sp => {
            var dep = sp.GetService<ISomeDependency>();
            return new MyService(dep);
        });

        // 实例注册
        container.RegisterSingleton<MyConfig>(new MyConfig());
    }
}
```

**注册到框架**：创建 `.asset` 后拖入 `BootConfig.asset` 的 `globalInstallers`（全局）或 `sceneInstallers`（按场景）。

### 5.4 事件系统

```csharp
// 1. 定义事件 — 必须是 struct
public struct MyCustomEvent
{
    public string SomeId;
    public int SomeValue;
}

// 2. 发布
[Inject] private IEventCenter _events;
_events.Publish(new MyCustomEvent { SomeId = "xxx", SomeValue = 42 });

// 3. 订阅
_events.Subscribe<MyCustomEvent>(e => {
    Debug.Log($"收到: {e.SomeId} = {e.SomeValue}");
});

// 4. 取消订阅（务必在 Dispose/OnShutdown 中取消）
_events.Unsubscribe<MyCustomEvent>(handler);
```

**已定义的事件**（位于 `Core/Events/EventDefinitions/EventStructs.cs`）：
- `ItemCollectedEvent { ItemID }`
- `PuzzleSolvedEvent { PuzzleID }`
- `DialogueEndedEvent { DialogueID }`
- `InteractionPerformedEvent { InteractableID }`
- `TriggerEnterEvent { TriggerID }`
- `StoryBeatCompletedEvent { StoryBeatID }`

### 5.5 ViewModel 创建与 View 代码绑定

**ViewModel（纯 C#，DI 注册）**：

```csharp
public class MyViewModel : ViewModelBase
{
    private string _displayText;
    public string DisplayText
    {
        get => _displayText;
        set => SetProperty(ref _displayText, value);
    }

    public ICommand DoSomethingCommand { get; private set; }

    public override void Initialize()
    {
        DoSomethingCommand = new RelayCommand(() => {
            DisplayText = "Done!";
        });
    }
}
```

**View（MonoBehaviour，挂场景 Canvas 上）**：

```csharp
public class MyView : StrictLifecycleMonoBehaviour
{
    [Inject] private MyViewModel _viewModel;

    [Header("UI 引用")]
    [SerializeField] private Button _doSomethingButton;
    [SerializeField] private Text _displayText;

    protected override void OnStartExternal()
    {
        _doSomethingButton.onClick.AddListener(() => _viewModel.DoSomethingCommand.Execute(null));

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MyViewModel.DisplayText))
                _displayText.text = _viewModel.DisplayText;
        };
    }

    protected override void OnShutdown()
    {
        _doSomethingButton.onClick.RemoveAllListeners();
    }
}
```

**场景中的操作**：
1. Canvas 根节点挂 `MyView` 组件
2. 把 Button / Text 拖到 Inspector 中对应的 `[SerializeField]` slot
3. **不需要**挂 `PropertyBinding` / `CommandBinding`，不填任何字符串

**和旧 Inspector 反射方式对比**：

| | Inspector 字符串绑定 | 代码 View 绑定 |
|---|---|---|
| 场景操作 | 挂 Binding 组件 + 填类型名字符串 + 填属性名字符串 | 拖 Button / Text 到 slot |
| 编译期安全 | 字符串拼错 → 运行时崩 | 改名 → 编译器报错 |
| 调试 | 反射链路上排查 | 断点打在 AddListener 上 |
| 适用场景 | 大型团队 / 策划调整绑定 | 本项目（小型团队） |

> **现有 `PropertyBinding` / `CommandBinding` 保留但不再作为主要开发路径。新 UI 一律使用 View 模式。**

### 5.6 存档接口

```csharp
// ISaveManager - 已完整实现
public interface ISaveManager
{
    bool SaveExists();
    GameSaveDto LoadSave();           // 无存档时自动创建默认存档
    void WriteSave(GameSaveDto data);
    GameSaveDto CreateNewSave();      // 创建从 Phase1 开始的默认存档
}

// GameSaveDto — [Serializable] 纯数据类
// currentPhase / currentSceneId / completedBeatIds / collectedItemIds /
// solvedPuzzleIds / completedDialogueIds / playTimeSeconds / saveDateTime
```

### 5.7 输入接口

```csharp
// IPlayerInput — 已实现，基于轮询（polling）
public interface IPlayerInput
{
    Vector2 MoveDirection { get; }  // 当前帧 WASD 方向，未按下时 Vector2.zero
    Vector2 MousePosition { get; }  // 当前鼠标位置
    bool IsClickTriggered { get; }  // 当前帧是否点击（WasPressedThisFrame）
    void Enable();
    void Disable();
}
```

通过 `[Inject] private IPlayerInput _input;` 获取，PlayerView 在 Update 中每帧轮询。当前键位：WASD 移动 + 鼠标左键点击。

### 5.8 场景交互接口（InteractableObject）

在场景 GameObject 上挂 `InteractableObject` 组件，配置：
- `InteractableType`：Collectible / Puzzle / Interactive / TriggerZone
- `TargetId`：用于 Beat 匹配的字符串 ID
- `TriggerOnly`：true=触发区域(OnTriggerEnter2D) / false=点击交互(OnMouseDown)
- `InteractOnce`：是否仅触发一次
- `PlayerTag`：触发区域模式下的玩家 Tag

**自动行为**：发布对应事件到 EventCenter → GameFlowManager 接收 → Beat 匹配。

---

## 6. ScriptableObject 配置体系

### 6.1 当前 InstallerConfig 实际配置

**`Assets/Configs/Boot/BootConfig.asset`**（Addressables label: `"BootConfig"`）：

```
globalInstallers:
  1. CoreInstaller.asset       → EventCenter, PlayerInput, ViewModelFactory, ScopeProvider
  2. GamePlayInstaller.asset    → GameFlowManager, PlayerManager, SaveManager, GameFlowModel

sceneInstallers:
  1. StartSceneInstaller.asset  → MainMenuViewModel (Scoped)
```

### 6.2 已创建的配置资产状态

| 文件 | 用途 | 填充状态 |
|------|------|---------|
| `Configs/Boot/BootConfig.asset` | Installer 总配置 | ✅ 4个global + 1个scene (StartSceneInstaller) 已配置 |
| `Configs/Boot/StartSceneInstaller.asset` | Start 场景 Installer | ✅ 已创建并添加到 BootConfig.sceneInstallers |
| `Configs/Boot/CoreInstaller.asset` | Core 服务注册 | ✅ 已配置 |
| `Configs/Boot/GamePlayInstaller.asset` | Gameplay 服务注册 | ✅ 已配置 |

| `Configs/Game Flow Model.asset` | 运行时 Model | ✅ 已创建 |
| `Configs/GameSaveData.asset` | 存档 SO（可能未使用） | ✅ 已创建 |
| `Configs/StoryBeats/Intro.asset` | 示例 StoryBeat | ❌ 所有字段为空 |
| `Configs/StoryPhase/StartPhase.asset` | 示例 GamePhaseConfig | ❌ 所有字段为空 |

### 6.3 如何创建新阶段

1. **创建 StoryBeat SO**：`Create → Gameplay → Story Beat`，填写 BeatId / Type / TargetId / Description
2. **创建 GamePhaseConfig SO**：`Create → SO → GamePhaseConfig`，填写 PhaseId / SceneAssetPath / RequiredBeats / DefaultNextPhase / 转场参数
3. **给 SO 打 Addressables label**：在 Inspector 中给 `GamePhaseConfig` 添加 label `"GamePhaseConfig"`（代码中通过此 label 加载）
4. **在对应场景中放置 InteractableObject**：配置 TargetId 与 StoryBeat.TargetId 一致

---

## 7. 当前场景与场景 Installer 机制

### 7.1 场景清单

| 场景文件 | 用途 | 状态 |
|----------|------|------|
| `Scenes/Start.unity` | 主菜单 | 需搭建 UI（MainMenuViewModel 已就绪） |
| `Scenes/OutterWorld.unity` | 表世界（客厅） | 需搭建场景内容 + 交互物 |
| `Scenes/InnerWorld_Park.unity` | 里世界·公园 | 需搭建场景内容 + 交互物 |

### 7.2 场景 Installer 工作原理

1. 在 `BootConfig.sceneInstallers` 中列出场景级 Installer（如 `StartSceneInstaller`）
2. `ProjectContext.SetupSceneScoping()` 为初始场景预创建 Scope 并运行 sceneInstallers
3. 后续场景切换时，`SceneScopeRunner.OnSceneLoaded()` 自动创建新 Scope 并运行 sceneInstallers
4. 场景卸载时 `SceneScopeRunner.OnSceneUnloaded()` 自动清理 Scope（释放所有 Scoped 服务）

`StartSceneInstaller.asset` 已添加到 `BootConfig.sceneInstallers`，场景加载时自动运行。

---

## 8. 已实现 vs 未实现（完整清单）

### 已实现并跑通 ✅

| 模块 | 状态 |
|------|------|
| DI 容器（Singleton/Scoped/Transient/注入/作用域/循环检测/依赖验证） | ✅ 完整 |
| 生命周期系统（IInitializable/IStartable/ITickable + LifecycleRegistry + StrictLifecycleMonoBehaviour） | ✅ 完整 |
| 启动流程（ProjectBootstrap → ProjectContext → Installers → 生命周期 → 场景管理） | ✅ 完整 |
| Installer 系统（ScriptableObject + InstallerConfig + order 排序） | ✅ 完整 |
| 事件总线（EventManager: Subscribe/Unsubscribe/Publish，强类型 struct 事件） | ✅ 完整 |
| SceneScopeRunner（场景加载时创建 Scope + 运行 SceneInstaller，卸载时清理） | ✅ 完整 |
| UpdateRunner（驱动 ITickable） | ✅ 完整 |
| 存档系统（JSON 文件读写，GameSaveDto ↔ GameSaveDataRuntime 互转） | ✅ 完整 |
| 输入系统（Unity InputSystem 封装，WASD + 鼠标点击） | ✅ 完整 |
| PropertyBinding / CommandBinding（反射属性绑定 + UI 事件绑定 + DI 解析 ViewModel） | ✅ 完整 |
| ViewModelBase（INotifyPropertyChanged + SetProperty + Command 工厂） | ✅ 完整 |
| RelayCommand / AsyncCommand（同步/异步 ICommand） | ✅ 完整 |
| GameFlowManager（事件订阅 → Beat 匹配 → 阶段推进 → 存档恢复） | ✅ 核心逻辑完整 |
| GameFlowView（转场 + 场景加载，但 BGM/对话/Fade 是 TODO） | ⚠️ 骨架完整 |
| InteractableObject（点击交互 + 触发区域 + 事件发布 + 视觉反馈） | ✅ 完整 |
| MainMenuViewModel（新游戏/继续游戏 → Addressables 加载场景） | ✅ 完整 |
| MainMenuView（代码绑定 UI，[Inject] ViewModel → OnStartExternal 连线） | ✅ 新建 |
| 启动时序修复（SetupSceneScoping 移到 ExecuteLifecycle 之前 + 注入失败重试） | ✅ 已修复 |
| GameFlowModel（MVVM Model，PhaseProgress 可绑定 Slider） | ✅ 完整 |
| GamePhase 枚举（10个阶段全部定义） | ✅ 完整 |

### 未实现 ❌

#### 阻塞游戏可玩（高优先级）

| 模块 | 当前状态 | 需要做什么 |
|------|---------|-----------|
| **音频管理器** (`IAudioManager`) | 不存在 | 创建接口 + 实现（BGM/SFX），注册到 CoreInstaller |
| **对话系统** (`IDialogueManager`) | 不存在 | 创建接口 + 实现（逐行显示/选项分支/自动推进），注册到 DI |
| **转场动画** (FadeToBlack/FadeFromBlack) | GameFlowView 中为 TODO | 在 GameFlowView 中实现 Canvas 黑屏淡入淡出 |
| **GamePhaseConfig 数据填充** | 只有空白的 StartPhase.asset | 为每个 GamePhase 创建 SO 并填充场景路径/Beats/下一阶段 |
| **StoryBeat 数据填充** | 只有空白的 Intro.asset | 为每个阶段创建所有 StoryBeat SO |
| **Addressables label 配置** | 资源未打 label | 给 GamePhaseConfig SO 打上 "GamePhaseConfig" label |

#### 完善游戏体验（中优先级）

| 模块 | 当前状态 | 需要做什么 |
|------|---------|-----------|
| **背包/道具系统** (`IInventoryManager`) | 不存在 | 创建接口 + 实现（收集/查询/分类），接入存档 |
| **结局判定** (`IEndingDeterminer`) | GameFlowManager 中有 TODO 注释 | 创建判定逻辑（根据收集的道具决定结局 A/B） |
| **场景搭建** | 3个场景都是空的 | 放置背景、交互物、UI Canvas |
| **主菜单场景搭建** | MainMenuViewModel + MainMenuView 代码已就绪 | 在 Start.unity 中建 Canvas，挂 MainMenuView，拖 Button/Slider 到 slot |
| **HUD** | 尚未实现 | 创建 HUDViewModel + HUDView（进度条/道具提示） |

#### 技术债务/优化（低优先级）

| 问题 | 说明 |
|------|------|
| **GlobalTime 空壳** | 空类文件 |
| **BindingManager 未被使用** | PropertyBinding/CommandBinding 自己管理生命周期，BindingManager 是死代码或为未来准备 |
| **GameFlowView 对话集成** | EntryDialogueId / ExitDialogueId 的播放逻辑全是 TODO |
| **单元测试** | 无 |

---

## 9. 推进优先级建议

### 第一步：打通 Phase1 最小可玩循环

1. 创建 Phase1 的 GamePhaseConfig（填充 Phase1_SurfaceLivingRoom_Initial）
2. 创建对应 StoryBeat（如：靠近猫碗 → EnterTrigger beat）
3. 给 SO 打 Addressables label "GamePhaseConfig"
4. 在 OutterWorld.unity 中放置背景 + 一个 InteractableObject（测试用）
5. 手动测试：启动 → Phase1 就绪 → 点击交互物 → Beat 完成 → 阶段推进

### 第二步：实现对话系统

6. 创建 IDialogueManager 接口 + DialogueManager 实现（最简版本：Text 组件显示+隐藏）
7. 注册到 DI（GamePlayInstaller 或新建 DialogueInstaller）
8. 接入 GameFlowView 的 EntryDialogueId/ExitDialogueId 播放

### 第三步：实现转场 + 音频

9. 实现 FadeToBlack / FadeFromBlack（Canvas 遮罩 + 协程/async）
10. 创建 IAudioManager + AudioManager（最简版本：AudioSource.PlayOneShot）
11. 接入 GameFlowView

### 第四步：完善所有阶段 + UI

12. 为所有 10 个 GamePhase 创建配置
13. 搭建所有场景 + 交互物
14. 搭建主菜单 UI（绑定 MainMenuViewModel）
15. 结局判定逻辑
16. 背包系统 + HUD

### 日常开发检查清单

- 新增服务 → 创建/修改 Installer → 确认已加入 BootConfig
- 新增场景 → 场景 Installer 加入 BootConfig.sceneInstallers
- 新增事件 → 在 EventStructs.cs 同目录添加 struct
- 新增 ViewModel → 继承 ViewModelBase → 注册到 DI
- 场景交互物 → 挂 InteractableObject，配置 Type + TargetId
- 策划配置 → 创建 GamePhaseConfig SO + StoryBeat SO → 打 Addressables label

---

## 附录：完整文件路径索引

```
Assets/
├── Core/
│   ├── DI/DIContainer.cs              # IoC 容器
│   ├── DI/Inject.cs                   # [Inject] 标记 + 注入逻辑
│   ├── Boot/ProjectBootstrap.cs       # 编辑器入口
│   ├── Boot/ProjectContext.cs         # 启动主流程
│   ├── Boot/SceneScopeRunner.cs       # 场景作用域管理
│   ├── Boot/UpdateRunner.cs           # Tick 驱动
│   ├── Architecture/LifecycleRegistry.cs    # 全局生命周期
│   ├── Architecture/LifecycleMonoBehaviour.cs # 严格 MB 基类
│   ├── Architecture/InstallerAsset.cs       # Installer 基类
│   ├── Architecture/InstallerConfig.cs      # 总配置 SO
│   ├── Architecture/ScopeProvider.cs        # 当前 Scope 追踪
│   ├── Architecture/Interfaces/IInitializable.cs
│   ├── Architecture/Interfaces/IStartable.cs
│   ├── Architecture/Interfaces/ITickable.cs
│   ├── Architecture/Installers/CoreInstaller.cs
│   ├── Events/EventManager.cs
│   ├── Events/EventInterfaces/IEventCenter.cs
│   ├── Events/EventDefinitions/EventStructs.cs
│   └── GlobalTime.cs                 # 空壳
├── MVVM/
│   ├── View/                      # View 层（代码绑定 UI）
│   │   └── MainMenuView.cs        # 主菜单 View
│   ├── ViewModel/Base/ViewModelBase.cs
│   ├── ViewModel/MainMenuViewModel.cs
│   ├── ViewModel/Factory/ViewModelFactory.cs
│   ├── ViewModel/Interfaces/IViewModelFactory.cs
│   ├── Binding/PropertyBinding.cs
│   ├── Binding/CommandBinding.cs
│   ├── Binding/BindingManager.cs        # 已实现但未接入
│   ├── Commands/RelayCommand.cs
│   ├── Commands/AsyncCommand.cs
│   └── Interfaces/IViewModel.cs
├── Gameplay/
│   ├── SceneFlow/GameFlowManager.cs
│   ├── SceneFlow/GameFlowView.cs
│   ├── SceneFlow/GamePhase.cs
│   ├── SceneFlow/StoryBeat.cs
│   ├── SO/GamePhaseConfig.cs
│   ├── SO/GameFlowModel.cs
│   ├── SO/GameSaveData.cs
│   ├── Save/SaveManager.cs
│   ├── Save/GameSaveDto.cs
│   ├── Save/ISaveManager.cs
│   ├── Interactions/InteractableObject.cs
│   ├── Interfaces/IGameFlowManager.cs
│   ├── Installer/GamePlayInstaller.cs
│   └── Installer/StartSceneInstaller.cs
├── Input/
│   ├── Manager/PlayerInputManager.cs    # 纯 C# 类，轮询 InputAction
│   ├── InputConfig/PlayerInput.cs       # 自动生成
│   └── InputInterface/IPlayerInput.cs
├── Configs/
│   ├── Boot/BootConfig.asset
│   ├── Boot/CoreInstaller.asset
│   ├── Boot/GamePlayInstaller.asset
│   ├── StoryBeats/Intro.asset          # 空白
│   └── StoryPhase/StartPhase.asset     # 空白
└── Scenes/
    ├── Start.unity
    ├── OutterWorld.unity
    └── InnerWorld_Park.unity
```

---

*文档更新日期：2026-05-05 | 基于 main 分支实际代码状态 | 最近提交：3ccd234*
