# CLAUDE.md — Unsaid Goodbye 项目开发约束与约定

> 本文档是开发本项目的基本遵循。所有代码生成、修改、审查都必须遵守。违反约束的代码不得合入。

---

## 1. 生命周期：禁用 Unity 原生消息

**规则**：永远不要在子类中直接使用 `Awake()` / `Start()` / `Update()`。

- MonoBehaviour 继承 `StrictLifecycleMonoBehaviour`，它封存了 Awake/Start 为 `private`
- 子类重写以下受保护方法：

| 方法 | 调用时机 | 用途 |
|------|---------|------|
| `OnInitialize()` | DI 注入完成后，所有组件 Initialize 之前 | 内部状态初始化、配置校验 |
| `OnStartExternal()` | 所有组件 Initialize 完成后 | 跨组件交互、事件订阅、UI 绑定 |
| `Tick(float dt)` | 每帧 | 输入轮询、Transform 同步 |
| `OnShutdown()` | OnDestroy 时 | 事件取消订阅、资源释放 |

- 纯 C# 服务通过接口接入生命周期：`IInitializable` / `IStartable` / `ITickable` / `IDisposable`
- 纯 C# 服务的 `IDisposable.Dispose()` 在 Scope 释放或容器 Dispose 时自动调用

---

## 2. DI 容器使用

### 2.1 注册

- 全局单例：`RegisterSingleton<IService, Impl>()` 或 `RegisterSingleton<T>(instance)`
- 随场景释放：`RegisterScoped<ViewModel, ViewModel>()`（场景 Installer 中注册）
- 每次新建：`RegisterTransient<T, Impl>()`
- **工厂注册**仅在转换/适配场景使用，正常类型注册优先用类型参数

### 2.2 注入

```csharp
[Inject] private ISomeService _service;           // 必须存在
[InjectOptional] private ISomeOptionalService _opt; // 可选
```

- 构造函数优先选 `[Inject]` 标记的，否则选参数最多的
- 字段和属性上标记 `[Inject]` 的也会被注入
- 依赖获取用接口，不要依赖具体类型

### 2.3 核心约束

- **`RegisterSingleton<T, Impl>()` 的 `Impl` 必须是具体类**（DI 容器在注册时校验，禁止接口/抽象类）
- **同一实例注册多个接口时，用一个变量持有，分别注册**：

```csharp
// ✅ 正确
var eventManager = new EventManager();
container.RegisterSingleton<IEventCenter>(eventManager);
container.RegisterSingleton<IInitializable>(eventManager);

// ❌ 错误 — "实现"是对另一个接口的转型
container.RegisterSingleton<IInitializable>(sp => (IInitializable)sp.GetRequiredService<IEventCenter>());
```

- 单例存储 key = 具体实现 `Type`，不是 descriptor ID。两个接口描述符指向同一个 `typeof(ConcreteClass)` key，自动命中同一实例
- Scoped 服务同理，`Scope.ScopedInstances` 以 `Type` 为 key
- 不要在 Installer 之外直接 `new` 需要 DI 的服务

---

## 3. MVVM 架构约束

### 3.1 分层

```
View (MonoBehaviour)  →  仅持有 UI 引用 + 绑定逻辑，不含业务逻辑
ViewModel (纯 C#)     →  状态 + 命令，通过 INotifyPropertyChanged 通知 View
Model (纯 C#)         →  数据和纯计算，不依赖任何 Unity API
Manager (纯 C#)       →  业务逻辑协调，注入 IEventCenter / ISaveManager 等服务
```

### 3.2 View 代码绑定模式

- **View 必须继承 `StrictLifecycleMonoBehaviour`**
- `[Inject]` 获取 ViewModel/Manager
- 在 `OnStartExternal()` 中做代码绑定（`onClick.AddListener` / `PropertyChanged +=`）
- 在 `OnShutdown()` 中解绑（`RemoveAllListeners` / `PropertyChanged -=`）
- **不新建 `PropertyBinding` / `CommandBinding` 组件**（旧的 Inspector 字符串绑定模式已废弃）
- **会修改 Transform 的 View**（如 PlayerView）：在 `OnInitialize()` 记录原始位置，`OnShutdown()` 恢复。防止 Play 模式退出后误保存将运行时坐标写回场景文件

### 3.3 ViewModel

- 继承 `ViewModelBase`（提供 `SetProperty<T>()` + `INotifyPropertyChanged` + Command 工厂）
- 不与 Unity API 耦合（不引入 `UnityEngine` 命名空间）

---

## 4. 安装器系统

- 新增服务 → 创建/修改 Installer → 确认 Installer 已加入 `BootConfig.asset`
- 全局单例服务 → `CoreInstaller` 或 `GamePlayInstaller`，放在 `globalInstallers`
- 场景级 Scoped 服务 → 场景专用 Installer，放在 `sceneInstallers`
- 每个 Installer 是 ScriptableObject（`[CreateAssetMenu]`），重写 `Register(DIContainer)`

---

## 5. 事件系统

- 所有事件 **必须是 struct**（`IEventCenter` 是强类型事件总线）
- 定义在 `Core/Events/EventDefinitions/` 目录
- 事件中引用交互物使用 **`InteractableId`**（ScriptableObject），不用 string。匹配走引用相等，杜绝拼写错误
- 订阅方在 `OnShutdown()` 或 `Dispose()` 中取消订阅，避免内存泄漏和幽灵调用
- 发布：`_events.Publish(new MyEvent { ... })`
- 订阅：`_events.Subscribe<MyEvent>(handler)`

---

## 6. 输入系统

- 输入是**轮询式**（polling），不是事件驱动
- `IPlayerInput` 暴露每帧读取的属性：`MoveDirection` / `MousePosition` / `IsClickTriggered`
- `PlayerInputManager` 是纯 C# 类，不要继承 MonoBehaviour
- View 在 `Tick()` 中轮询输入 → 写入 Manager → 同步 Transform

---

## 7. 场景与作用域

- 场景切换用 `Addressables.LoadSceneAsync`（不用 `SceneManager.LoadScene`）
- 场景 Service 注册为 Scoped → 场景卸载时随 Scope 自动释放
- `SceneScopeRunner` 监听 `SceneManager.sceneLoaded/Unloaded` 自动管理 Scope 生命周期
- 新场景必须有对应的 Installer，加入 `BootConfig.sceneInstallers`

---

## 8. 命名空间与文件组织

| 层 | 命名空间 | 目录 |
|----|---------|------|
| DI 容器 | `Core.DI` | `Core/DI/` |
| 生命周期 | `Core.Architecture` | `Core/Architecture/` |
| 事件 | `Core.Events` | `Core/Events/` |
| 启动引导 | `Core.Boot` | `Core/Boot/` |
| MVVM | `MVVM.*` | `MVVM/` |
| 输入 | `Input.*` | `Input/` |
| 游戏逻辑 | `Gameplay.*` | `Gameplay/` |

---

## 9. 调试

- `DIContainer.VerboseDebug = true` 开启完整 DI 链路日志（构造函数参数、注入字段、生命周期注册）
- `LifecycleRegistry.DumpState()` 打印当前所有已注册组件及其状态
- DI 容器在 Boot 阶段自动执行 `Validate()` 验证依赖图，启动时有任何解析失败都会打印 `[DI Validate] FAIL`

---

## 10. 禁止事项清单

- ❌ 不要在子类中写 `void Awake()` / `void Start()` / `void Update()`
- ❌ 不要新建 `PropertyBinding` / `CommandBinding` 组件（用 View 代码绑定替代）
- ❌ 不要直接在非 Installer 处 `new` 一个需要 `[Inject]` 的服务
- ❌ 不要在事件订阅后忘记取消订阅
- ❌ 不要用 `SceneManager.LoadScene`（用 Addressables）
- ❌ 不要让 ViewModel 引用 `UnityEngine` 命名空间
- ❌ 不要把 Scoped 服务注册为 Singleton（会导致场景卸载后残留）
- ❌ 不要把接口/抽象类作为 `RegisterSingleton<T, Impl>()` 的 Impl 参数
- ❌ 不要在 `RegisterSingleton<Iface>(factory)` 的工厂里转型另一个服务接口
