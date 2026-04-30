using System;
using Core.Architecture;
using Core.DI;
using Gameplay.Interfaces;
using Gameplay.SO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace Gameplay.SceneFlow
{
    /// <summary>
    /// 剧情流程 View — 表现层
    /// 订阅 Controller 事件，执行转场动画、场景加载、BGM、对话
    /// 自身不暴露属性给 PropertyBinding（UI 绑定直接挂 GameFlowModel.asset 作为 _source）
    /// </summary>
    public class SceneFlowView : StrictLifecycleMonoBehaviour
    {
        [Inject] private IGameFlowController _controller;

        // TODO: 待子系统就绪后注入
        // [Inject] private IAudioManager _audio;
        // [Inject] private IDialogueManager _dialogue;

        private const string PhaseConfigLabel = "GamePhaseConfig";

        protected override void OnStartExternal()
        {
            _controller.OnPhaseComplete += HandlePhaseComplete;
            _controller.OnPhaseChanged += HandlePhaseChanged;

            Debug.Log("[SceneFlowView] Subscribed to GameFlow events");
        }

        protected override void OnShutdown()
        {
            _controller.OnPhaseComplete -= HandlePhaseComplete;
            _controller.OnPhaseChanged -= HandlePhaseChanged;
        }

        private async void HandlePhaseComplete(GamePhase nextPhase)
        {
            var config = await LoadPhaseConfig(nextPhase);
            if (config == null) return;

            Debug.Log($"[SceneFlowView] Phase complete, transitioning: {_controller.CurrentPhase} → {nextPhase}");

            // 1. 播放离开对话
            if (!string.IsNullOrEmpty(config.ExitDialogueId))
            {
                // TODO: await _dialogue.PlayAndWait(config.ExitDialogueId);
                await WaitSeconds(0.5f);
            }

            // 2. 黑屏淡入
            // TODO: await FadeToBlack(config.TransitionDuration);
            Debug.Log($"[SceneFlowView] Fade to black ({config.TransitionDuration}s)");

            // 3. 加载场景
            if (!string.IsNullOrEmpty(config.SceneAssetPath))
            {
                var asyncOp = SceneManager.LoadSceneAsync(config.SceneAssetPath);
                if (asyncOp != null)
                {
                    asyncOp.allowSceneActivation = true;
                    while (!asyncOp.isDone)
                        await System.Threading.Tasks.Task.Yield();
                }
            }

            // 4. 通知 Controller 状态切换完成
            _controller.ConfirmTransition(nextPhase);
        }

        private async void HandlePhaseChanged(GamePhase newPhase)
        {
            var config = await LoadPhaseConfig(newPhase);
            if (config == null) return;

            Debug.Log($"[SceneFlowView] Phase changed to: {newPhase} ({config.DisplayName})");

            // 5. 切换 BGM
            // TODO: _audio.PlayBGM(config.BackgroundMusic);

            // 6. 黑屏淡出
            // TODO: await FadeFromBlack(config.TransitionDuration);
            Debug.Log($"[SceneFlowView] Fade from black ({config.TransitionDuration}s)");

            // 7. 播放进入对话
            if (!string.IsNullOrEmpty(config.EntryDialogueId))
            {
                // TODO: await _dialogue.PlayAndWait(config.EntryDialogueId);
                await WaitSeconds(0.5f);
            }
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
                Debug.LogError($"[SceneFlowView] Failed to load PhaseConfig for {phase}");
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
            Debug.LogError($"[SceneFlowView] PhaseConfig not found for {phase}");
            return null;
        }

        private async System.Threading.Tasks.Task WaitSeconds(float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                await System.Threading.Tasks.Task.Yield();
            }
        }
    }
}
