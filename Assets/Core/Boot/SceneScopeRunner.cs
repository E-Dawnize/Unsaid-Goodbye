using Core.Architecture;
using Core.Architecture.Interfaces;
using Core.DI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Boot
{
    /// <summary>
    /// 场景作用域管理器
    /// - 场景加载时创建 Scope，注入场景 Installer，设到 IScopeProvider
    /// - 场景卸载时清理 Scope
    /// - 初始场景的 Scope 由 ProjectContext 在 Boot 后预先创建
    /// </summary>
    public class SceneScopeRunner : MonoBehaviour
    {
        private InstallerConfig _config;
        private IScope _scope;
        private DIContainer _globalContainer;
        private IScopeProvider _scopeProvider;

        public static void Attach(InstallerConfig config, DIContainer globalContainer, IScopeProvider scopeProvider)
        {
            var go = new GameObject("SceneScopeRunner");
            DontDestroyOnLoad(go);
            var runner = go.AddComponent<SceneScopeRunner>();
            runner._config = config;
            runner._globalContainer = globalContainer;
            runner._scopeProvider = scopeProvider;
            SceneManager.sceneLoaded += runner.OnSceneLoaded;
            SceneManager.sceneUnloaded += runner.OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 如果已有 Scope（初始场景由 ProjectContext 创建），复用，不重建
            if (_scope != null) return;

            CreateScopeAndInit();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            _scopeProvider.CurrentScope = null;
            _scope?.Dispose();
            _scope = null;
        }

        /// <summary>
        /// 创建 Scope 并初始化场景级服务
        /// </summary>
        public void CreateScopeAndInit()
        {
            _scope = _globalContainer.CreateScope();
            _scopeProvider.CurrentScope = _scope;

            // 安装场景级注册
            foreach (var installer in _config.SceneInstallersSorted)
                installer.Register(_globalContainer);

            // 初始化场景 Scoped 服务
            InitializeScoped();
            StartScoped();
        }

        private void InitializeScoped()
        {
            var scope = _scope as DIContainer.Scope;
            foreach (var init in _globalContainer.ResolveAll<IInitializable>(scope))
                init.Initialize();
        }

        private void StartScoped()
        {
            var scope = _scope as DIContainer.Scope;
            foreach (var start in _globalContainer.ResolveAll<IStartable>(scope))
                start.OnStart();
        }
    }
}
