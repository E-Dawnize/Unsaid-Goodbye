using System.Threading.Tasks;
using System.Windows.Input;
using Core.Architecture;
using Core.DI;
using Gameplay.Interfaces;
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
        [Inject] private IGameFlowManager _gameFlow;
        private AsyncOperationHandle<SceneInstance> _sceneHandle;
        private bool _sceneLoaded;

        public ICommand StartGameCommand { get; private set; }
        public ICommand ContinueGameCommand { get; private set; }

        private float _volume = 1f;
        public float Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        public override void Initialize()
        {
            StartGameCommand = new AsyncCommand(StartGameAsync);
            ContinueGameCommand = new AsyncCommand(ContinueGameAsync);
        }

        /// <summary>新游戏：强制创建新存档，从 Phase1 开始</summary>
        private async Task StartGameAsync()
        {
            var save = _saveManager.CreateNewSave();
            Debug.Log("[MainMenu] 新游戏开始");

            await LoadAndEnterScene(save.currentPhase);
        }

        /// <summary>继续游戏：仅当存档存在时有效。通关后从 Phase1 重开但保留道具。</summary>
        private async Task ContinueGameAsync()
        {
            if (!_saveManager.SaveExists())
            {
                Debug.LogWarning("[MainMenu] 没有存档，无法继续游戏");
                return;
            }

            var save = _saveManager.LoadSave();
            Debug.Log($"[MainMenu] 继续游戏: Phase={save.currentPhase}");

            // 通关后继续 → 从 Phase1 重开，保留道具
            if (save.currentPhase == GamePhase.Phase7_Epilogue_A ||
                save.currentPhase == GamePhase.Phase7_Epilogue_B)
            {
                Debug.Log("[MainMenu] 存档已通关，从 Phase1 重开并保留道具");
                var newSave = GameSaveDto.CreateDefault();
                newSave.collectedItemIds = save.collectedItemIds; // 保留道具
                _saveManager.WriteSave(newSave);
                await LoadAndEnterScene(GamePhase.Phase1_SurfaceLivingRoom_Initial);
                return;
            }

            await LoadAndEnterScene(save.currentPhase);
        }

        private async Task LoadAndEnterScene(GamePhase phase)
        {
            var scenePath = await LoadScenePathForPhase(phase);

            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError($"[MainMenu] 无法找到阶段 {phase} 的场景路径");
                return;
            }

            Debug.Log($"[MainMenu] 加载场景: {scenePath}");
            _sceneHandle = Addressables.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            await _sceneHandle.Task;

            if (_sceneHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[MainMenu] 场景加载失败: {scenePath}");
                return;
            }

            _sceneLoaded = true;
            Debug.Log($"[MainMenu] 场景加载完成，通知 GameFlowManager 初始化阶段");
            _gameFlow.OnGameSceneLoaded();
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
            // 不释放 _sceneHandle：场景加载中释放会取消加载，加载完成后释放会卸载游戏场景
            base.Dispose();
        }
    }
}
