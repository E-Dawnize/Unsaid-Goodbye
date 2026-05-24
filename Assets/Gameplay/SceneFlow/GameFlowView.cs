using System;
using Core.Architecture;
using Core.DI;
using Core.Events.EventInterfaces;
using Gameplay.Audio;
using Gameplay.Dialogue;
using Gameplay.Interfaces;
using Gameplay.SO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gameplay.SceneFlow
{
    /// <summary>
    /// 剧情流程 View — 表现层
    /// 订阅 Manager 事件，执行转场动画、场景加载、BGM、对话
    /// 自身不暴露属性给 PropertyBinding（UI 绑定直接挂 GameFlowModel.asset 作为 _source）
    /// </summary>
    public class GameFlowView : StrictLifecycleMonoBehaviour
    {
        [Inject] private IGameFlowManager _manager;
        [Inject] private IDialogueManager _dialogue;
        [Inject] private IAudioManager _audio;
        [Inject] private IEventCenter _events;

        private AsyncOperationHandle<SceneInstance> _sceneHandle;
        private static GameFlowView _instance;
        private CanvasGroup _fadeGroup;
        private bool _playedCurrentEntryDialogue;
        private const string PhaseConfigLabel = "GamePhaseConfig";

        protected override void OnInitialize()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureFadeOverlay();
        }

        protected override void OnStartExternal()
        {
            if (_instance != this) return;

            _manager.OnPhaseComplete += HandlePhaseComplete;
            _manager.OnPhaseChanged += HandlePhaseChanged;
            _events.Subscribe<SceneLoadRequest>(HandleSceneLoad);
            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log("[GameFlowView] Subscribed to GameFlow events");
            PlayCurrentEntryDialogueIfNeeded();
        }

        protected override void OnShutdown()
        {
            if (_manager != null)
            {
                _manager.OnPhaseComplete -= HandlePhaseComplete;
                _manager.OnPhaseChanged -= HandlePhaseChanged;
            }

            _events?.Unsubscribe<SceneLoadRequest>(HandleSceneLoad);
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_instance == this)
                _instance = null;

            if (_sceneHandle.IsValid())
                Addressables.Release(_sceneHandle);
        }

        /// <summary>场景加载后回调 — 主菜单切游戏场景时触发阶段初始化</summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameFlowView] Scene loaded: {scene.name}, mode={mode}, manager={_manager != null}");
            if (_manager != null)
                _manager.OnGameSceneLoaded();
            else
                Debug.LogError("[GameFlowView] Cannot init phase: _manager is null");
        }

        private async void HandlePhaseComplete(GamePhase nextPhase)
        {
            if (nextPhase == GamePhase.None) return;
            var config = await LoadPhaseConfig(nextPhase);
            if (config == null) return;

            Debug.Log($"[GameFlowView] Phase complete, transitioning: {_manager.CurrentPhase} → {nextPhase}");

            // 1. 播放离开对话
            if (!string.IsNullOrEmpty(config.ExitDialogueId))
            {
                await _dialogue.PlayAndWait(config.ExitDialogueId);
            }

            // 2. 转场音效
            if (_audio != null)
                _audio.PlaySfx("SFX/Transition");

            // 3. 黑屏淡入
            await FadeToBlack(config.TransitionDuration);

            // 4. 加载场景
            if (!string.IsNullOrEmpty(config.SceneAssetPath))
            {
                // 释放上一场景的 handle
                if (_sceneHandle.IsValid())
                    Addressables.Release(_sceneHandle);

                Debug.Log($"[GameFlowView] Loading scene: {config.SceneAssetPath}");
                _sceneHandle = Addressables.LoadSceneAsync(config.SceneAssetPath, LoadSceneMode.Single);
                await _sceneHandle.Task;

                if (_sceneHandle.Status != AsyncOperationStatus.Succeeded)
                    Debug.LogError($"[GameFlowView] Failed to load scene: {config.SceneAssetPath}");
            }

            // 4. 通知 Manager 状态切换完成
            _manager.ConfirmTransition(nextPhase);

            // 5. 自动存档
            _manager.GetSaveState();
        }

        private async void HandlePhaseChanged(GamePhase newPhase)
        {
            if (newPhase == GamePhase.None) return; // 非游戏场景，无需加载配置

            var config = await LoadPhaseConfig(newPhase);
            if (config == null) return;

            Debug.Log($"[GameFlowView] Phase changed to: {newPhase} ({config.DisplayName})");

            // 5. 切换 BGM
            if (!string.IsNullOrEmpty(config.BackgroundMusic))
                _audio.PlayBgm(config.BackgroundMusic);

            // 6. 黑屏淡出
            await FadeFromBlack(config.TransitionDuration);

            // 7. 播放进入对话
            if (!string.IsNullOrEmpty(config.EntryDialogueId))
            {
                _playedCurrentEntryDialogue = true;
                await _dialogue.PlayAndWait(config.EntryDialogueId);
            }
        }

        private async void PlayCurrentEntryDialogueIfNeeded()
        {
            if (_playedCurrentEntryDialogue || _manager == null)
                return;

            var waitedFrames = 0;
            while (!_playedCurrentEntryDialogue &&
                   _manager.CurrentPhase == GamePhase.None &&
                   waitedFrames < 120)
            {
                waitedFrames++;
                await System.Threading.Tasks.Task.Yield();
            }

            if (_playedCurrentEntryDialogue)
                return;

            if (_manager.CurrentPhase == GamePhase.None)
            {
                Debug.LogWarning("[GameFlowView] Entry dialogue skipped: current phase is not ready.");
                return;
            }

            var config = _manager.CurrentConfig ?? await LoadPhaseConfig(_manager.CurrentPhase);
            if (config == null)
            {
                Debug.LogWarning($"[GameFlowView] Entry dialogue skipped: no config for {_manager.CurrentPhase}.");
                return;
            }

            if (string.IsNullOrEmpty(config.EntryDialogueId))
            {
                Debug.Log($"[GameFlowView] Entry dialogue skipped: no EntryDialogueId for {_manager.CurrentPhase}.");
                return;
            }

            _playedCurrentEntryDialogue = true;
            Debug.Log($"[GameFlowView] Playing current phase entry dialogue: {config.EntryDialogueId}");
            await _dialogue.PlayAndWait(config.EntryDialogueId);
        }

        private async System.Threading.Tasks.Task<GamePhaseConfig> LoadPhaseConfig(GamePhase phase)
        {
            var handle = Addressables.LoadAssetsAsync<GamePhaseConfig>(
                PhaseConfigLabel,
                null,
                false
            );
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[GameFlowView] Failed to load PhaseConfig for {phase}");
                return null;
            }

            foreach (var config in handle.Result)
            {
                if (config.PhaseId == phase)
                {
                    Addressables.Release(handle);
                    return config;
                }
            }

            Addressables.Release(handle);
            Debug.LogError($"[GameFlowView] PhaseConfig not found for {phase}");
            return null;
        }

        private void EnsureFadeOverlay()
        {
            if (_fadeGroup != null) return;

            var canvasObject = new GameObject("GameFlowFadeOverlay");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            _fadeGroup = canvasObject.AddComponent<CanvasGroup>();
            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = false;
            _fadeGroup.interactable = false;

            var imageObject = new GameObject("Black");
            imageObject.transform.SetParent(canvasObject.transform, false);

            var image = imageObject.AddComponent<Image>();
            image.color = Color.black;

            var rect = image.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private System.Threading.Tasks.Task FadeToBlack(float duration)
            => Fade(1f, duration, true);

        private System.Threading.Tasks.Task FadeFromBlack(float duration)
            => Fade(0f, duration, false);

        private async System.Threading.Tasks.Task Fade(float targetAlpha, float duration, bool blockRaycasts)
        {
            EnsureFadeOverlay();

            Debug.Log($"[GameFlowView] Fade {(targetAlpha > 0f ? "to" : "from")} black ({duration}s)");

            _fadeGroup.blocksRaycasts = true;

            var startAlpha = _fadeGroup.alpha;
            if (duration <= 0f)
            {
                _fadeGroup.alpha = targetAlpha;
                _fadeGroup.blocksRaycasts = blockRaycasts && targetAlpha > 0f;
                return;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                await System.Threading.Tasks.Task.Yield();
            }

            _fadeGroup.alpha = targetAlpha;
            _fadeGroup.blocksRaycasts = blockRaycasts && targetAlpha > 0f;
        }

        private async void HandleSceneLoad(SceneLoadRequest e)
        {
            Debug.Log($"[GameFlowView] Scene load requested: {e.ScenePath}");
            await FadeToBlack(1f);

            if (_sceneHandle.IsValid())
                Addressables.Release(_sceneHandle);

            _sceneHandle = Addressables.LoadSceneAsync(e.ScenePath, LoadSceneMode.Single);
            await _sceneHandle.Task;

            await FadeFromBlack(1f);
        }
    }
}
