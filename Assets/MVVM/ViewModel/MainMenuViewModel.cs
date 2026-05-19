using System.Threading.Tasks;
using System.Windows.Input;
using Core.Architecture;
using Core.DI;
using Gameplay.Save;
using Gameplay.SceneFlow;
using Gameplay.SO;
using MVVM.Commands;
using MVVM.ViewModel.Base;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MVVM.ViewModel
{
    /// <summary>
    /// 主菜单 ViewModel — 由 MainMenuView 通过 [Inject] 注入后在代码中绑定
    /// </summary>
    public class MainMenuViewModel : ViewModelBase
    {
        [Inject] private ISaveManager _saveManager;
        private AsyncOperationHandle<SceneInstance> _sceneHandle;

        public ICommand StartGameCommand { get; private set; }

        private float _volume = 1f;
        public float Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        public override void Initialize()
        {
            StartGameCommand = new AsyncCommand(StartGameAsync);
        }

        private async Task StartGameAsync()
        {
            // 1. 获取存档：存在则继续，不存在则创建新存档
            GameSaveDto save;
            if (_saveManager.SaveExists())
            {
                save = _saveManager.LoadSave();
                Debug.Log($"[MainMenu] 继续游戏: Phase={save.currentPhase}");
            }
            else
            {
                save = _saveManager.CreateNewSave();
                Debug.Log("[MainMenu] 新游戏开始");
            }

            // 2. 加载阶段配置，获取目标场景路径
            var scenePath = await LoadScenePathForPhase(save.currentPhase);

            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError($"[MainMenu] 无法找到阶段 {save.currentPhase} 的场景路径");
                return;
            }

            // 3. 加载游戏场景
            Debug.Log($"[MainMenu] 加载场景: {scenePath}");
            _sceneHandle = Addressables.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            await _sceneHandle.Task;

            if (_sceneHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[MainMenu] 场景加载失败: {scenePath}");
            }
        }

        private static async Task<string> LoadScenePathForPhase(GamePhase phase)
        {
            var handle = Addressables.LoadAssetsAsync<GamePhaseConfig>(
                "GamePhaseConfig", null, false);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[MainMenu] 加载 GamePhaseConfig 失败");
                return null;
            }

            foreach (var config in handle.Result)
            {
                if (config.PhaseId == phase)
                {
                    var path = config.SceneAssetPath;
                    Addressables.Release(handle);
                    return path;
                }
            }

            Addressables.Release(handle);
            Debug.LogError($"[MainMenu] 未找到阶段 {phase} 的配置");
            return null;
        }

        public override void Dispose()
        {
            if (_sceneHandle.IsValid())
                Addressables.Release(_sceneHandle);
            base.Dispose();
        }
    }
}
