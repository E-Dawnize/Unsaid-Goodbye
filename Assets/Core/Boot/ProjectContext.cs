using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Architecture;
using Core.Architecture.Interfaces;
using Core.DI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Boot
{
    /// <summary>
    /// 项目启动入口 - 集成新的生命周期管理系统
    /// 核心职责：
    /// 1. 初始化DI容器
    /// 2. 设置LifecycleRegistry
    /// 3. 协调全局生命周期执行顺序
    /// 4. 管理项目级Scope
    /// </summary>
    public class ProjectContext : MonoBehaviour
    {
        private static ProjectContext _instance;
        private DIContainer _globalContainer;
        private IScope _projectScope;
        private InstallerConfig _loadedConfig;
        readonly string _installerAssertLabel="BootConfig";
        /// <summary>
        /// 确保ProjectContext存在并已启动
        /// </summary>
        public static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject("ProjectContext");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ProjectContext>();
            go.AddComponent<UpdateRunner>();
            _instance.Boot();
        }

        /// <summary>
        /// 获取当前DI容器（供其他系统使用）
        /// </summary>
        //public static DIContainer GetContainer() => _instance?._globalContainer;

        /// <summary>
        /// 获取项目级Scope
        /// </summary>
        public static IScope GetProjectScope() => _instance?._projectScope;

        private async void Boot()
        {
            Debug.Log("[ProjectContext] Starting boot sequence...");

            // 阶段1: 创建DI容器和Scope
            CreateDIContainer();

            // 阶段2: 注册全局安装器
            await RegisterInstallers();

            // 阶段2.5: 依赖图验证 — 提前暴露循环依赖/缺失注册等问题
            ValidateDependencies();

            // 阶段3: 设置LifecycleRegistry
            SetupLifecycleRegistry();

            // 阶段4: 启动场景作用域管理 + 为初始场景预创建 Scope
            // 必须在 ExecuteLifecycle 之前执行，确保 Scoped ViewModel 在 DI 注入时已注册
            SetupSceneScoping();

            // 阶段5: 执行生命周期初始化
            ExecuteLifecycle();

            // 阶段6: 启动游戏循环
            StartGameLoop();

            Debug.Log("[ProjectContext] Boot sequence completed");
        }

        #region 启动阶段实现
        private void CreateDIContainer()
        {
            _globalContainer = new DIContainer();
            _projectScope = _globalContainer.CreateScope();
            Debug.Log("[ProjectContext] DI container and project scope created");
        }

        private async Task RegisterInstallers()
        {
            var handle=Addressables.LoadAssetAsync<InstallerConfig>(_installerAssertLabel).Task;
            _loadedConfig = await handle;
            if (_loadedConfig != null)
            {
                foreach (var installer in _loadedConfig.GlobalInstallersSorted)
                {
                    installer.Register(_globalContainer);
                }
                Debug.Log($"[ProjectContext] Registered {_loadedConfig.GlobalInstallersSorted.Count()} global installers");
            }
            else
            {
                Debug.LogWarning("[ProjectContext] No installer config found");
            }
        }

        private void ValidateDependencies()
        {
            Debug.Log("[ProjectContext] Validating dependency graph...");
            var result = _globalContainer.Validate();
            if (!result.IsValid)
            {
                var msg = $"[ProjectContext] DI validation failed with {result.Errors.Count} error(s):\n" +
                          string.Join("\n", result.Errors);
                Debug.LogError(msg);
            }
        }

        private void SetupLifecycleRegistry()
        {
            // 设置LifecycleRegistry使用我们的DI容器
            LifecycleRegistry.SetContainer(_globalContainer, _projectScope);
            Debug.Log("[ProjectContext] LifecycleRegistry configured");
        }

        private void ExecuteLifecycle()
        {
            // 关键保证：所有Initialize完成 → 所有OnStart开始

            // 步骤0: 预注册所有生命周期服务
            PreRegisterLifecycleServices();

            // 1. 初始化阶段
            Debug.Log("[ProjectContext] Starting Initialize phase...");
            LifecycleRegistry.InitializeAll();

            // 2. 启动阶段
            Debug.Log("[ProjectContext] Starting OnStart phase...");
            LifecycleRegistry.StartAll();
        }

        /// <summary>
        /// 预注册所有需要生命周期管理的服务
        /// 确保所有IInitializable和IStartable服务被解析并注册到LifecycleRegistry
        /// </summary>
        private void PreRegisterLifecycleServices()
        {
            Debug.Log("[ProjectContext] Pre-registering lifecycle services...");

            try
            {
                // 解析所有IInitializable服务（这会触发创建和注册）
                var initializables = _globalContainer.ResolveAll<IInitializable>(_projectScope);
                Debug.Log($"[ProjectContext] Pre-registered {initializables.Count()} IInitializable services");

                // 解析所有IStartable服务
                var startables = _globalContainer.ResolveAll<IStartable>(_projectScope);
                Debug.Log($"[ProjectContext] Pre-registered {startables.Count()} IStartable services");

                // 注意：一个服务可能同时实现IInitializable和IStartable，解析时会创建一次
                // DI容器的CreateInstance方法会自动注册到LifecycleRegistry
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProjectContext] Failed to pre-register lifecycle services: {ex.Message}");
                throw;
            }
        }

        private void StartGameLoop()
        {
            // 注册ITickable组件到UpdateRunner
            var updateRunner = GetComponent<UpdateRunner>();
            if (updateRunner == null)
            {
                updateRunner = gameObject.AddComponent<UpdateRunner>();
            }

            // 获取所有ITickable服务并注册
            var tickables = _globalContainer.ResolveAll<ITickable>(_projectScope);
            foreach (var tickable in tickables)
            {
                updateRunner.Register(tickable);
            }

            Debug.Log($"[ProjectContext] Game loop started with {tickables.Count()} tickable components");
        }

        /// <summary>
        /// 启动场景作用域管理系统
        /// 1. 为即将加载的初始场景预创建 Scope（保证场景 Awake 时有 Scope 可用）
        /// 2. 挂载 SceneScopeRunner 处理后续场景切换
        /// </summary>
        private void SetupSceneScoping()
        {
            if (_loadedConfig == null)
            {
                Debug.LogWarning("[ProjectContext] No InstallerConfig, skipping scene scoping");
                return;
            }

            var scopeProvider = _globalContainer.GetService<IScopeProvider>();
            if (scopeProvider == null)
            {
                Debug.LogWarning("[ProjectContext] IScopeProvider not registered, skipping scene scoping");
                return;
            }

            // 为初始场景预创建 Scope
            var initialScope = _globalContainer.CreateScope();
            scopeProvider.CurrentScope = initialScope;

            // 注入场景 Installer
            foreach (var installer in _loadedConfig.SceneInstallersSorted)
                installer.Register(_globalContainer);

            // 初始化场景级 Scoped 服务
            var scope = initialScope as DIContainer.Scope;
            foreach (var init in _globalContainer.ResolveAll<IInitializable>(scope))
                init.Initialize();

            foreach (var start in _globalContainer.ResolveAll<IStartable>(scope))
                start.OnStart();

            // 挂载 SceneScopeRunner，处理后续场景切换
            SceneScopeRunner.Attach(_loadedConfig, _globalContainer, scopeProvider);

            Debug.Log("[ProjectContext] Scene scoping initialized — initial scope created for first scene");
        }
        #endregion

        #region 清理
        private void OnDestroy()
        {
            // 清理LifecycleRegistry
            LifecycleRegistry.Clear();

            // 释放DI容器
            _projectScope?.Dispose();
            _globalContainer?.Dispose();

            Debug.Log("[ProjectContext] Cleanup completed");
        }
        #endregion
    }
}