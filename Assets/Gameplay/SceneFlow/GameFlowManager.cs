using System;
using System.Collections.Generic;
using System.Linq;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
using Gameplay.Interfaces;
using Gameplay.Inventory;
using Gameplay.Save;
using Gameplay.SO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

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
        [Inject] private IInventoryManager _inventory;

        private const string PhaseConfigLabel = "GamePhaseConfig";
        private const GamePhase DefaultStartPhase = GamePhase.Phase1_SurfaceLivingRoom_Initial;

        private Dictionary<GamePhase, GamePhaseConfig> _configs;
        private GamePhaseConfig _currentConfig;
        private HashSet<InteractionDef> _completedBeats = new();
        private readonly List<InteractionDef> _pendingBeatCompletions = new();
        private AsyncOperationHandle<IList<GamePhaseConfig>> _loadHandle;
        private bool _configsLoaded;
        /// <summary>所有已知 InteractionDef 的 name→引用 映射（从 RequiredBeats 收集），用于存档恢复</summary>
        private readonly Dictionary<string, InteractionDef> _allKnownDefs = new();

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

            if (TryGetPhaseForActiveScene(out var activeScenePhase))
            {
                // 当前场景匹配某个游戏阶段 → 正常启动或恢复
                if (GameData != null && GameData.CurrentPhase == activeScenePhase)
                {
                    RestoreFromSave();
                }
                else
                {
                    if (GameData != null && GameData.CurrentPhase != GamePhase.None)
                        Debug.Log($"[GameFlow] Active scene overrides saved phase: {GameData.CurrentPhase} -> {activeScenePhase}");

                    StartPhase(activeScenePhase);
                }
            }
            else
            {
                // 不在游戏场景中（如主菜单）→ 保持空闲，等待 OnGameSceneLoaded 触发
                Debug.Log("[GameFlow] Not in a game scene, waiting for scene load...");
            }
        }
        #endregion

        #region 事件订阅
        private void SubscribeEvents()
        {
            _events.Subscribe<DialogueEndedEvent>(e =>
            {
                TryCompleteBeat(e.Def);
                GetSaveState(); // 每次交互后存档
            });
        }
        #endregion

        #region Beat 匹配（纯逻辑）
        private void TryCompleteBeat(InteractionDef def)
        {
            if (def == null)
                return;

            if (_model.IsTransitioning)
            {
                QueuePendingBeat(def, "transitioning");
                return;
            }

            if (_currentConfig == null)
            {
                QueuePendingBeat(def, "current config is null");
                return;
            }

            bool changed = false;
            foreach (var required in _currentConfig.RequiredBeats)
            {
                if (IsSameBeat(required, def))
                {
                    if (_completedBeats.Add(required))
                    {
                        changed = true;
                        Debug.Log($"[GameFlow] Beat 完成: {required.name}");
                        _events.Publish(new StoryBeatCompletedEvent { StoryBeatID = required.name });
                    }
                }
            }

            if (changed)
            {
                _model.ApplyBeatProgress(_completedBeats.Count, _currentConfig.RequiredBeats.Count);
                Debug.Log($"[GameFlow] Beat progress: {def.name} ({_completedBeats.Count}/{_currentConfig.RequiredBeats.Count})");

                if (AllBeatsDone())
                {
                    _model.SetTransitioning(true);
                    var nextPhase = DetermineNextPhase();
                    Debug.Log($"[GameFlow] Phase complete: {CurrentPhase} -> {nextPhase}");
                    OnPhaseComplete?.Invoke(nextPhase);
                }
            }
            else
            {
                var requiredNames = string.Join(", ", _currentConfig.RequiredBeats.Select(beat => beat != null ? beat.name : "<null>"));
                Debug.LogWarning($"[GameFlow] Beat ignored for current phase {CurrentPhase}: {def.name}. Required: {requiredNames}");
            }
        }

        private static bool IsSameBeat(InteractionDef required, InteractionDef completed)
        {
            if (required == null || completed == null)
                return false;

            if (required == completed)
                return true;

            return string.Equals(required.name, completed.name, StringComparison.Ordinal);
        }

        private bool AllBeatsDone()
            => _currentConfig.RequiredBeats.All(required =>
                _completedBeats.Any(completed => IsSameBeat(required, completed)));

        private void QueuePendingBeat(InteractionDef def, string reason)
        {
            if (def == null)
                return;

            if (_pendingBeatCompletions.Any(pending => IsSameBeat(pending, def)))
                return;

            _pendingBeatCompletions.Add(def);
            Debug.LogWarning($"[GameFlow] Beat queued because {reason}: {def.name}");
        }
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
            FlushPendingBeats();

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
            FlushPendingBeats();
        }

        private bool TryGetPhaseForActiveScene(out GamePhase phase)
        {
            phase = GamePhase.None;

            var activeSceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(activeSceneName) || _configs == null)
                return false;

            var matchingConfigs = _configs.Values
                .Where(config => ScenePathMatchesActiveScene(config.SceneAssetPath, activeSceneName))
                .ToList();

            if (matchingConfigs.Count != 1)
                return false;

            phase = matchingConfigs[0].PhaseId;
            return phase != GamePhase.None;
        }

        private static bool ScenePathMatchesActiveScene(string sceneAssetPath, string activeSceneName)
        {
            if (string.IsNullOrEmpty(sceneAssetPath))
                return false;

            var normalizedPath = sceneAssetPath.Replace("\\", "/");
            return normalizedPath == activeSceneName || normalizedPath.EndsWith($"/{activeSceneName}");
        }

        private void StartDefaultPhase()
        {
            StartPhase(DefaultStartPhase);
        }
        #endregion

        #region 存档
        /// <summary>
        /// 游戏场景加载后调用 — 重新读档并尝试匹配当前场景启动阶段
        /// 主菜单 → 游戏场景时由 GameFlowView 通过 SceneManager.sceneLoaded 触发
        /// </summary>
        public async void OnGameSceneLoaded()
        {
            Debug.Log($"[GameFlow] OnGameSceneLoaded: currentConfig={_currentConfig != null}, configs={_configs?.Count ?? 0}");

            if (_currentConfig != null)
            {
                if (TryGetPhaseForActiveScene(out var curPhase) && curPhase == CurrentPhase)
                {
                    Debug.Log($"[GameFlow] Scene loaded but phase already active ({CurrentPhase}), skipping init");
                    return;
                }
                // 加载的是非游戏场景（如主菜单），重置阶段状态，隐藏背包/暂停按钮
                _currentConfig = null;
                _model.ApplyPhase(GamePhase.None, 0, 0);
                OnPhaseChanged?.Invoke(GamePhase.None);
                Debug.Log("[GameFlow] Phase reset — loaded a non-game scene");
                return;
            }

            // 确保 configs 已加载（处理竞态：用户在主菜单点击过快时 configs 可能尚未就绪）
            if (!_configsLoaded)
            {
                Debug.Log("[GameFlow] Configs not yet loaded, awaiting...");
                try
                {
                    await LoadPhaseConfigs();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameFlow] Failed to load configs on scene load: {e}");
                    return;
                }
            }

            // 重新读档（可能已被 MainMenuViewModel 覆盖）
            LoadSaveData();
            Debug.Log($"[GameFlow] Save reloaded: phase={GameData?.CurrentPhase}, items={GameData?.CollectedItemIds?.Count ?? 0}");

            if (TryGetPhaseForActiveScene(out var activeScenePhase))
            {
                Debug.Log($"[GameFlow] Active scene matches phase: {activeScenePhase}, saved phase: {GameData?.CurrentPhase}");
                if (GameData != null && GameData.CurrentPhase == activeScenePhase)
                {
                    RestoreFromSave();
                }
                else
                {
                    if (GameData != null && GameData.CurrentPhase != GamePhase.None)
                        Debug.Log($"[GameFlow] Active scene overrides saved phase: {GameData.CurrentPhase} -> {activeScenePhase}");

                    StartPhase(activeScenePhase);
                }
            }
            else
            {
                Debug.LogWarning($"[GameFlow] Scene loaded but doesn't match any phase config. Active scene: {SceneManager.GetActiveScene().name}");
            }
        }

        private void RestoreFromSave()
        {
            var phase = GameData.CurrentPhase;
            if (!_configs.TryGetValue(phase, out var config))
            {
                Debug.LogWarning($"[GameFlow] Saved phase has no config: {phase}. Starting default phase.");
                StartDefaultPhase();
                return;
            }

            _currentConfig = config;
            // 从 save 的 string ID (def.name) 还原为 InteractionDef 引用
            var defByName = config.RequiredBeats.ToDictionary(d => d.name, d => d);
            _completedBeats = new HashSet<InteractionDef>(
                (GameData.CompletedBeatIds ?? Enumerable.Empty<string>())
                    .Select(id => defByName.TryGetValue(id, out var d) ? d : null)
                    .Where(d => d != null));
            _model.ApplyPhase(phase, _completedBeats.Count, config.RequiredBeats.Count);
            OnPhaseChanged?.Invoke(phase);
            FlushPendingBeats();

            RestoreInventoryFromSave();

            Debug.Log($"[GameFlow] 从存档恢复: {phase}");
        }

        private void RestoreInventoryFromSave()
        {
            if (GameData.CollectedItemIds == null || GameData.CollectedItemIds.Count == 0)
                return;

            var count = 0;
            foreach (var itemId in GameData.CollectedItemIds.ToList())
            {
                if (_allKnownDefs.TryGetValue(itemId, out var def))
                {
                    _inventory.RestoreItem(def);
                    GameData.CollectedItemIds.Remove(itemId);
                    count++;
                }
            }

            if (GameData.CollectedItemIds.Count > 0)
            {
                Debug.LogWarning($"[GameFlow] {GameData.CollectedItemIds.Count} item(s) not found in known defs, not restored: {string.Join(", ", GameData.CollectedItemIds)}");
            }

            Debug.Log($"[GameFlow] Restored {count} items from save");
        }

        public GameSaveDataRuntime GetSaveState()
        {
            GameData.CurrentPhase = _model.CurrentPhase;
            GameData.CompletedBeatIds = new HashSet<string>(_completedBeats.Select(d => d.name));
            GameData.CollectedItemIds = new HashSet<string>(_inventory.CollectedItems.Select(d => d.name));
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

        private async System.Threading.Tasks.Task LoadAllInteractionDefs()
        {
            try
            {
                var handle = Addressables.LoadAssetsAsync<InteractionDef>("InteractionDef",
                    def => { if (def != null && !_allKnownDefs.ContainsKey(def.name)) _allKnownDefs[def.name] = def; },
                    false);
                await handle.Task;
                if (handle.IsValid()) Addressables.Release(handle);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameFlow] Failed to load InteractionDefs via label: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadPhaseConfigs()
        {
            if (_configsLoaded) return;

            _loadHandle = Addressables.LoadAssetsAsync<GamePhaseConfig>(
                PhaseConfigLabel,
                asset => _configs[asset.PhaseId] = asset,
                false
            );
            await _loadHandle.Task;

            if (_loadHandle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Failed to load PhaseConfigs: {_loadHandle.Status}");

            _configsLoaded = true;
            // 构建 InteractionDef 查找表（用于存档恢复）
            foreach (var config in _configs.Values)
            {
                foreach (var def in config.RequiredBeats)
                {
                    if (def != null && !_allKnownDefs.ContainsKey(def.name))
                        _allKnownDefs[def.name] = def;
                }
            }
            // 加载所有带 InteractionDef 标签的道具（包括不在 RequiredBeats 中的隐藏道具 H0-H4 等）
            await LoadAllInteractionDefs();
            Debug.Log($"[GameFlow] Loaded {_configs.Count} phase configs, {_allKnownDefs.Count} known defs");
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_loadHandle.IsValid())
                Addressables.Release(_loadHandle);

            _configs?.Clear();
            _completedBeats?.Clear();
            _pendingBeatCompletions?.Clear();
        }
        #endregion

        private void FlushPendingBeats()
        {
            if (_pendingBeatCompletions.Count == 0)
                return;

            var pendingBeats = _pendingBeatCompletions.ToArray();
            _pendingBeatCompletions.Clear();

            foreach (var pendingBeat in pendingBeats)
                TryCompleteBeat(pendingBeat);
        }
    }
}
