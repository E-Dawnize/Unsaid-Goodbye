using System;
using System.Threading.Tasks;
using Core.Architecture;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Gameplay.Interstitial
{
    /// <summary>
    /// 全屏插屏组件 — 在场景切换间隙展示单张 Addressable 图片
    /// 流程：加载 sprite → 淡入 → 等待最低展示时间 + 玩家点击 → 淡出 → 完成
    /// 作为 DontDestroyOnLoad 全局单例注册
    /// </summary>
    public class InterstitialScreen : StrictLifecycleMonoBehaviour, IInterstitialScreen
    {
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Image _image;
        private Text _hintText;
        private bool _clicked;
        private bool _isShowing;
        private AsyncOperationHandle<Sprite> _spriteHandle;

        protected override void OnInitialize()
        {
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        protected override void Tick(float dt)
        {
            if (!_isShowing) return;

            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            bool pressed;
            if (mouse != null) pressed = mouse.leftButton.wasPressedThisFrame;
            else if (touch != null) pressed = touch.primaryTouch.press.wasPressedThisFrame;
            else return;

            if (pressed)
                _clicked = true;
        }

        // ==================== Public API ====================

        public async Task ShowAsync(string addressableKey, float minDisplaySeconds = 2f)
        {
            if (string.IsNullOrEmpty(addressableKey))
            {
                Debug.LogWarning("[InterstitialScreen] ShowAsync called with empty key, skipping");
                return;
            }

            if (_isShowing)
            {
                Debug.LogWarning("[InterstitialScreen] Already showing, ignoring duplicate call");
                return;
            }

            _isShowing = true;
            _clicked = false;

            // 加载 sprite
            Sprite sprite = null;
            try
            {
                _spriteHandle = Addressables.LoadAssetAsync<Sprite>(addressableKey);
                await _spriteHandle.Task;

                if (_spriteHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    sprite = _spriteHandle.Result;
                }
                else
                {
                    Debug.LogWarning($"[InterstitialScreen] Failed to load sprite '{addressableKey}', showing black");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InterstitialScreen] Exception loading '{addressableKey}': {ex.Message}");
            }

            // 应用 sprite（失败时保持黑色）
            if (sprite != null)
            {
                _image.sprite = sprite;
                _image.color = Color.white;
            }
            else
            {
                _image.sprite = null;
                _image.color = Color.black;
            }

            // 隐藏提示文字
            if (_hintText != null)
                _hintText.gameObject.SetActive(false);

            // 显示并淡入
            _canvas.gameObject.SetActive(true);
            _canvasGroup.blocksRaycasts = true;
            await FadeCanvas(0f, 1f, 0.5f);

            // 等待最低展示时间
            var startTime = Time.time;
            var hintShown = false;
            while (!_clicked)
            {
                var elapsed = Time.time - startTime;

                // 最低展示时间过后显示提示
                if (!hintShown && elapsed >= minDisplaySeconds && _hintText != null)
                {
                    hintShown = true;
                    _hintText.gameObject.SetActive(true);
                }

                await Task.Yield();
            }

            // 兜底：即使 sprite 加载失败，确保至少展示了 minDisplaySeconds
            var totalElapsed = Time.time - startTime;
            if (totalElapsed < minDisplaySeconds)
            {
                var remaining = minDisplaySeconds - totalElapsed;
                var waitStart = Time.time;
                while (Time.time - waitStart < remaining)
                    await Task.Yield();
            }

            // 淡出
            _canvasGroup.blocksRaycasts = false;
            await FadeCanvas(1f, 0f, 0.3f);

            // 清理
            _canvas.gameObject.SetActive(false);
            _image.sprite = null;
            _image.color = Color.black;

            if (_spriteHandle.IsValid())
            {
                Addressables.Release(_spriteHandle);
            }

            _isShowing = false;
            Debug.Log($"[InterstitialScreen] Dismissed '{addressableKey}'");
        }

        // ==================== Internal ====================

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("InterstitialCanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = short.MaxValue - 1; // 比 GameFlowView 的 fade 低一级

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // 全屏图片
            var imgGo = new GameObject("Image");
            imgGo.transform.SetParent(canvasGo.transform, false);
            _image = imgGo.AddComponent<Image>();
            _image.color = Color.black;
            _image.preserveAspect = true; // 保持图片宽高比，不拉伸
            var imgRect = _image.GetComponent<RectTransform>();
            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.offsetMin = Vector2.zero;
            imgRect.offsetMax = Vector2.zero;

            // "点击继续" 提示文字
            var textGo = new GameObject("HintText");
            textGo.transform.SetParent(canvasGo.transform, false);
            _hintText = textGo.AddComponent<Text>();
            _hintText.text = "点击任意位置继续";
            _hintText.fontSize = 26;
            _hintText.alignment = TextAnchor.MiddleCenter;
            _hintText.color = new Color(1f, 1f, 1f, 0.6f);
            var textRect = _hintText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.06f);
            textRect.anchorMax = new Vector2(1f, 0.12f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // 尝试设置内置字体（Arial），失败则不显示文字
            var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtinFont != null)
                _hintText.font = builtinFont;

            canvasGo.SetActive(false);
        }

        private async Task FadeCanvas(float from, float to, float duration)
        {
            if (_canvasGroup == null) return;

            if (duration <= 0f)
            {
                _canvasGroup.alpha = to;
                return;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                await Task.Yield();
            }

            _canvasGroup.alpha = to;
        }

        protected override void OnShutdown()
        {
            _isShowing = false;

            if (_spriteHandle.IsValid())
                Addressables.Release(_spriteHandle);

            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }
    }
}
