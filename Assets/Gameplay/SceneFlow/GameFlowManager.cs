using System;
using System.Collections.Generic;
using System.Linq;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
using Gameplay.Interfaces;
using Gameplay.Save;
using Gameplay.SO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gameplay.SceneFlow
{
    /// <summary>
    /// 剧情流程 Manager — 纯业务逻辑，不碰表现层
    /// Beat 匹配、阶段判定、写入 Model、通知 View
    /// </summary>
    public class GameFlowManager : IGameFlowManager
    {
        [Inject] private IEventCenter _events;
        [Inject] private GameFlowModel _model;
        [Inject] private ISaveManager _saveManager;

        private const string PhaseConfigLabel = "GamePhaseConfig";

        private Dictionary<GamePhase, GamePhaseConfig> _configs;
        private GamePhaseConfig _currentConfig;
        private HashSet<StoryBeat> _completedBeats = new();
        private AsyncOperationHandle<IList<GamePhaseConfig>> _loadHandle;

        public GameSaveDataRuntime GameData { get; private set; }
        public GamePhase CurrentPhase => _model != null ? _model.CurrentPhase : GamePhase.None;
        public GamePhaseConfig CurrentConfig => _currentConfig;
        public event Action<GamePhase> OnPhaseChanged;
        public event Action<GamePhase> OnPhaseComplete;

        #region IInitializable
        public async void Initialize()
        {
            _configs = new Dictionary<GamePhase, GamePhaseConfig>();

            try
            {
                LoadSaveData();
                await LoadPhaseConfigs();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameFlow] Init failed: {e}");
                return;
            }

            SubscribeEvents();

            // 从存档恢复或从头开始
            if (GameData != null && GameData.CurrentPhase != GamePhase.None)
            {
                RestoreFromSave();
            }
            else
            {
                StartPhase(GamePhase.Phase1_SurfaceLivingRoom_Initial);
            }
        }
        #endregion

        #region 事件订阅
        private void SubscribeEvents()
        {
            _events.Subscribe<InteractionEvent>(e =>
                TryCompleteBeat(e.Def));

            _events.Subscribe<DialogueEndedEvent>(e =>
                TryCompleteBeat(e.Def));
        }
        #endregion

        #region Beat 匹配（纯逻辑）
        private void TryCompleteBeat(InteractionDef def)
        {
            if (_model.IsTransitioning || _currentConfig == null || def == null) return;

            bool changed = false;
            foreach (var beat in _currentConfig.RequiredBeats)
            {
                if (beat.Def == def)
                {
                    if (_completedBeats.Add(beat))
                    {
                        changed = true;
                        Debug.Log($"[GameFlow] Beat 完成: {beat.name} ({beat.Description})");
                        _events.Publish(new StoryBeatCompletedEvent { StoryBeatID = beat.name });
                    }
                }
            }

            if (changed)
            {
                _model.ApplyBeatProgress(_completedBeats.Count, _currentConfig.RequiredBeats.Count);

                if (AllBeatsDone())
                {
                    _model.SetTransitioning(true);
                    var nextPhase = DetermineNextPhase();
                    OnPhaseComplete?.Invoke(nextPhase);
                }
            }
        }

        private bool AllBeatsDone()
            => _currentConfig.RequiredBeats.All(b => _completedBeats.Contains(b));
        #endregion

        #region 阶段判定
        private GamePhase DetermineNextPhase()
        {
            if (_currentConfig.IsEndingBranch)
            {
                // TODO: 接入 IEndingDeterminer + IInventoryManager
                // var ending = _ending.DetermineEnding(_inventory);
                // return ending == EndingType.B_Leave
                //     ? GamePhase.Phase7_Epilogue_B
                //     : GamePhase.Phase7_Epilogue_A;
                return _currentConfig.DefaultNextPhase;
            }
            return _currentConfig.DefaultNextPhase;
        }
        #endregion

        #region View 回调 + 阶段切换
        /// <summary>
        /// View 在转场动画 + 场景加载完成后调用，通知 Controller 状态切换完成
        /// </summary>
        public void ConfirmTransition(GamePhase newPhase)
        {
            if (!_configs.TryGetValue(newPhase, out var config))
            {
                Debug.LogError($"[GameFlow] Phase config not found: {newPhase}");
                return;
            }

            _currentConfig = config;
            _completedBeats.Clear();
            _model.ApplyPhase(newPhase, 0, config.RequiredBeats.Count);
            OnPhaseChanged?.Invoke(newPhase);

            Debug.Log($"[GameFlow] 进入阶段: {newPhase} ({config.DisplayName})");
        }

        private void StartPhase(GamePhase phase)
        {
            if (!_configs.TryGetValue(phase, out var config))
            {
                Debug.LogError($"[GameFlow] Phase config not found: {phase}");
                return;
            }
            _currentConfig = config;
            _model.ApplyPhase(phase, 0, config.RequiredBeats.Count);
            OnPhaseChanged?.Invoke(phase);
        }
        #endregion

        #region 存档
        private void RestoreFromSave()
        {
            var phase = GameData.CurrentPhase;
            if (!_configs.TryGetValue(phase, out var config)) return;

            _currentConfig = config;
            // 从 save 的 string ID (beat.name) 还原为 StoryBeat 引用
            var beatByName = config.RequiredBeats.ToDictionary(b => b.name, b => b);
            _completedBeats = new HashSet<StoryBeat>(
                (GameData.CompletedBeatIds ?? Enumerable.Empty<string>())
                    .Select(id => beatByName.TryGetValue(id, out var b) ? b : null)
                    .Where(b => b != null));
            _model.ApplyPhase(phase, _completedBeats.Count, config.RequiredBeats.Count);
            OnPhaseChanged?.Invoke(phase);

            Debug.Log($"[GameFlow] 从存档恢复: {phase}");
        }

        public GameSaveDataRuntime GetSaveState()
        {
            GameData.CurrentPhase = _model.CurrentPhase;
            GameData.CompletedBeatIds = new HashSet<string>(_completedBeats.Select(b => b.name));
            _saveManager.WriteSave(GameData.ToDto());
            return GameData;
        }
        #endregion

        #region 资源加载
        private void LoadSaveData()
        {
            var dto = _saveManager.LoadSave(); // 有则读，无则自动创建默认存档
            GameData = new GameSaveDataRuntime(dto);
        }

        private async System.Threading.Tasks.Task LoadPhaseConfigs()
        {
            _loadHandle = Addressables.LoadAssetsAsync<GamePhaseConfig>(
                PhaseConfigLabel,
                asset => _configs[asset.PhaseId] = asset,
                false
            );
            await _loadHandle.Task;

            if (_loadHandle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Failed to load PhaseConfigs: {_loadHandle.Status}");

            Debug.Log($"[GameFlow] Loaded {_configs.Count} phase configs");
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_loadHandle.IsValid())
                Addressables.Release(_loadHandle);

            _configs?.Clear();
            _completedBeats?.Clear();
        }
        #endregion
    }
}
