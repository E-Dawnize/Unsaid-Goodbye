using System;
using System.Collections.Generic;
using Core.Architecture.Interfaces;
using Core.DI;
using Gameplay.Interfaces;
using Gameplay.Save;
using Gameplay.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Gameplay.SceneFlow;

namespace Gameplay.Pause
{
    public class PauseMenuManager : IInitializable, IDisposable, ITickable, IPauseMenu
    {
        [Inject] private IGameFlowManager _flow;
        [Inject] private ISaveManager _save;
        [Inject] private Inventory.IBackpackUI _backpack;
        [Inject] private Settings.SettingsManager _settings;
        [Inject] private Audio.IAudioManager _audio;

        public bool IsOpen { get; private set; }

        /// <summary>暂停菜单打开时屏蔽背包等底层 UI 的点击</summary>
        public static bool IsAnyOpen { get; private set; }

        private const string PrefabKey = "Arts/UI/Settings";
        private const string PopupLayer = "Popup";
        private static readonly Vector3 ToggleFixedOffset = new(8.5f, 4.5f, 5f);

        private Camera _cam;

        // Toggle button
        private GameObject _toggleGo;
        private SpriteRenderer _toggleSr;
        private BoxCollider2D _toggleCol;
        private bool _toggleHovered;
        private bool _togglePressed;

        // Overlay
        private GameObject _overlayGo;

        // Prefab
        private GameObject _menuRoot;
        private GameObject _backToMainGo;
        private GameObject _backGo;
        private GameObject _soundGo;
        private GameObject _screenGo;
        private GameObject _operateGo;
        private SpriteRenderer _backToMainSr;
        private SpriteRenderer _backSr;
        private SpriteRenderer _soundSr;
        private SpriteRenderer _screenSr;
        private SpriteRenderer _operateSr;
        private Color _soundOrigColor;
        private Color _screenOrigColor;
        private Color _operateOrigColor;
        private Color _backToMainOrigColor;
        private Color _backOrigColor;
        private bool _uiBuilt;

        public void Initialize()
        {
            _cam = Camera.main;
            _flow.OnPhaseChanged += OnPhaseChanged;
            if (_flow.CurrentConfig != null)
                BuildGameUI();
        }

        public void Dispose()
        {
            _flow.OnPhaseChanged -= OnPhaseChanged;
            if (_menuRoot != null) Object.Destroy(_menuRoot);
            if (_overlayGo != null) Object.Destroy(_overlayGo);
            if (_toggleGo != null) Object.Destroy(_toggleGo);
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (!_uiBuilt && _flow.CurrentConfig != null)
                BuildGameUI();
            SyncToggleVisibility();
        }

        private void BuildGameUI()
        {
            _uiBuilt = true;
            _cam = Camera.main;
            BuildOverlay();
            BuildToggleBtn();
            LoadPrefab();
        }

        private void SyncToggleVisibility()
        {
            var visible = _flow.CurrentConfig != null;
            if (_toggleGo != null) _toggleGo.SetActive(visible);
            if (!visible && IsOpen) Close();
        }

        // ==================== Overlay ====================

        private void BuildOverlay()
        {
            _overlayGo = new GameObject("PauseOverlay");
            _overlayGo.SetActive(false);
            Object.DontDestroyOnLoad(_overlayGo);

            var camZ = _cam != null ? _cam.transform.position.z : -10f;
            _overlayGo.transform.position = new Vector3(0f, 0f, camZ + 5f);

            var sr = _overlayGo.AddComponent<SpriteRenderer>();
            sr.sprite = CreateWhiteSprite();
            sr.color = new Color(0.02f, 0.01f, 0.04f, 0.55f);
            sr.sortingLayerName = PopupLayer;
            sr.sortingOrder = 105;

            if (_cam != null)
            {
                var halfH = _cam.orthographicSize;
                var halfW = halfH * _cam.aspect;
                _overlayGo.transform.localScale = new Vector3(halfW * 2f, halfH * 2f, 1f);
            }

        }

        // ==================== Toggle Button ====================

        private void BuildToggleBtn()
        {
            _toggleGo = new GameObject("PauseToggleBtn");
            _toggleGo.SetActive(false);
            Object.DontDestroyOnLoad(_toggleGo);

            _toggleSr = _toggleGo.AddComponent<SpriteRenderer>();
            _toggleSr.sortingLayerName = PopupLayer;
            _toggleSr.sortingOrder = 110;
            _toggleSr.sprite = CreatePauseIcon();
            _toggleSr.color = new Color(1f, 1f, 1f, 0.7f);

            _toggleCol = _toggleGo.AddComponent<BoxCollider2D>();
            _toggleCol.isTrigger = true;
            _toggleCol.size = new Vector2(0.6f, 0.6f);

            _toggleGo.layer = LayerMask.NameToLayer("UI");

            _toggleGo.transform.position = (Camera.main?.transform.position ?? Vector3.zero) + ToggleFixedOffset;
            _toggleGo.SetActive(false);
        }

        private void UpdateTogglePosition()
        {
            if (_cam == null || _toggleGo == null) return;
            _toggleGo.transform.position = _cam.transform.position + ToggleFixedOffset;
        }

        // ==================== Prefab ====================

        private async void LoadPrefab()
        {
            try
            {
                var assetHandle = Addressables.LoadAssetAsync<GameObject>(PrefabKey);
                await assetHandle.Task;

                if (assetHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[PauseMenu] Failed to load prefab: {PrefabKey}");
                    return;
                }

                _menuRoot = Object.Instantiate(assetHandle.Result);
                _menuRoot.SetActive(false);
                Object.DontDestroyOnLoad(_menuRoot);
                Addressables.Release(assetHandle);

                // 设置所有 SpriteRenderer 的 sorting layer（基准 106，高于背包的 100）
                foreach (var sr in _menuRoot.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.sortingLayerName = PopupLayer;
                    sr.sortingOrder = 106 + sr.sortingOrder;
                }

                // 找到按钮子物体（名称以 Prefab 中实际命名为准）
                _backToMainGo = FindChildRecursive(_menuRoot.transform, "从选区 图像_5");
                _backGo = FindChildRecursive(_menuRoot.transform, "从选区 图像");
                _soundGo = FindChildRecursive(_menuRoot.transform, "从选区 图像_2");
                _screenGo = FindChildRecursive(_menuRoot.transform, "从选区 图像_3");
                _operateGo = FindChildRecursive(_menuRoot.transform, "从选区 图像_4");
                Debug.Log($"[PauseMenu] Buttons found: back={_backGo != null}, backToMain={_backToMainGo != null}, sound={_soundGo != null}, screen={_screenGo != null}, operate={_operateGo != null}");

                if (_backToMainGo == null)
                    Debug.LogWarning($"[PauseMenu] Child 'backtomain' not found in prefab {PrefabKey}");
                else
                {
                    SetupClickTarget(_backToMainGo);
                    _backToMainSr = _backToMainGo.GetComponent<SpriteRenderer>();
                    if (_backToMainSr != null) _backToMainOrigColor = _backToMainSr.color;
                }

                if (_backGo == null)
                    Debug.LogWarning($"[PauseMenu] Child 'back' not found in prefab {PrefabKey}");
                else
                {
                    SetupClickTarget(_backGo);
                    _backSr = _backGo.GetComponent<SpriteRenderer>();
                    if (_backSr != null) _backOrigColor = _backSr.color;
                }

                if (_soundGo != null) SetupClickTarget(_soundGo);
                if (_screenGo != null)
                {
                    SetupClickTarget(_screenGo);
                    var col = _screenGo.GetComponent<BoxCollider2D>();
                    if (col != null) { col.offset = new Vector2(0, 1); col.size = new Vector2(6, 1.5f); }
                }
                if (_operateGo != null) SetupClickTarget(_operateGo);
                _soundSr = _soundGo != null ? _soundGo.GetComponent<SpriteRenderer>() : null;
                _screenSr = _screenGo != null ? _screenGo.GetComponent<SpriteRenderer>() : null;
                _operateSr = _operateGo != null ? _operateGo.GetComponent<SpriteRenderer>() : null;
                if (_soundSr != null) _soundOrigColor = _soundSr.color;
                if (_screenSr != null) _screenOrigColor = _screenSr.color;
                if (_operateSr != null) _operateOrigColor = _operateSr.color;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PauseMenu] Error loading prefab: {ex.Message}");
            }
        }

        private static GameObject FindChildRecursive(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;

                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void SetupClickTarget(GameObject go)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                col.size = sr.sprite.bounds.size;
            else
                col.size = Vector2.one;
            go.layer = LayerMask.NameToLayer("UI");
        }

        // ==================== Open / Close ====================

        public void Toggle()
        {
            if (!_uiBuilt) BuildGameUI();
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (!_uiBuilt) BuildGameUI();
            if (_menuRoot == null) return;
            if (_backpack != null && _backpack.IsOpen)
                _backpack.Close();
            IsOpen = true;
            IsAnyOpen = true;
            _overlayGo.SetActive(true);
            _menuRoot.SetActive(true);
            UpdateMenuPosition();
        }

        public void Close()
        {
            IsOpen = false;
            IsAnyOpen = false;
            if (_overlayGo != null) _overlayGo.SetActive(false);
            if (_menuRoot != null) _menuRoot.SetActive(false);
        }

        private void UpdateMenuPosition()
        {
            if (_menuRoot == null || _cam == null) return;
            var camPos = _cam.transform.position;
            _menuRoot.transform.position = new Vector3(camPos.x, camPos.y, camPos.z + 5f);
        }

        // ==================== Tick ====================

        void ITickable.Tick(float dt)
        {
            if (!_uiBuilt) return;

            if (_cam == null)
            {
                _cam = Camera.main;
                if (_cam == null) return;
            }

            // Escape 键切换
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Toggle();
                return;
            }

            UpdateTogglePosition();

            var mouse = Mouse.current;
            var touch = Touchscreen.current;

            // 获取指针位置和按下状态
            Vector2 pointerPos;
            bool isPointerDown;
            bool isPointerClicked;
            if (mouse != null)
            {
                pointerPos = mouse.position.ReadValue();
                isPointerDown = mouse.leftButton.isPressed;
                isPointerClicked = mouse.leftButton.wasPressedThisFrame;
            }
            else if (touch != null && touch.primaryTouch.press.isPressed)
            {
                pointerPos = touch.primaryTouch.position.ReadValue();
                isPointerDown = true;
                isPointerClicked = touch.primaryTouch.press.wasPressedThisFrame;
            }
            else
            {
                return;
            }

            var worldPos = (Vector2)_cam.ScreenToWorldPoint(pointerPos);
            var hits = new List<Collider2D>();
            Physics2D.OverlapPoint(worldPos, new ContactFilter2D().NoFilter(), hits);

            // Toggle button: 检查悬停 + 按下
            var isOverToggle = false;
            foreach (var hit in hits)
                if (hit.gameObject == _toggleGo) { isOverToggle = true; break; }

            var isPressToggle = isOverToggle && isPointerDown;

            // Toggle 颜色：按下 0.6，悬停 0.8
            Color toggleColor;
            if (isPressToggle)
                toggleColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            else if (isOverToggle)
                toggleColor = new Color(0.8f, 0.8f, 0.8f, 0.7f);
            else
                toggleColor = new Color(1f, 1f, 1f, 0.7f);

            if (_toggleHovered != isOverToggle || _togglePressed != isPressToggle)
            {
                _toggleHovered = isOverToggle;
                _togglePressed = isPressToggle;
                _toggleSr.color = toggleColor;
            }

            // Toggle 点击
            if (isOverToggle && isPointerClicked)
            {
                Toggle();
                return;
            }

            if (!IsOpen || Settings.SettingsManager.IsSettingsOpen) return;

            // Menu 内按钮
            var isOverBack = false;
            var isOverBackToMain = false;
            var isOverSound = false;
            var isOverScreen = false;
            var isOverOperate = false;
            var clickedSound = false;
            var clickedScreen = false;
            var clickedOperate = false;
            foreach (var hit in hits)
            {
                if (_backGo != null && hit.gameObject == _backGo) isOverBack = true;
                if (_backToMainGo != null && hit.gameObject == _backToMainGo) isOverBackToMain = true;
                if (_soundGo != null && hit.gameObject == _soundGo) isOverSound = true;
                if (_screenGo != null && hit.gameObject == _screenGo) isOverScreen = true;
                if (_operateGo != null && hit.gameObject == _operateGo) isOverOperate = true;
                if (isPointerClicked)
                {
                    if (_soundGo != null && hit.gameObject == _soundGo) clickedSound = true;
                    if (_screenGo != null && hit.gameObject == _screenGo) clickedScreen = true;
                    if (_operateGo != null && hit.gameObject == _operateGo) clickedOperate = true;
                }
            }

            // 按钮颜色更新
            UpdateButtonColor(_backSr, _backOrigColor, isOverBack, isOverBack && isPointerDown);
            UpdateButtonColor(_backToMainSr, _backToMainOrigColor, isOverBackToMain, isOverBackToMain && isPointerDown);
            UpdateButtonColor(_soundSr, _soundOrigColor, isOverSound, isOverSound && isPointerDown);
            UpdateButtonColor(_screenSr, _screenOrigColor, isOverScreen, isOverScreen && isPointerDown);
            UpdateButtonColor(_operateSr, _operateOrigColor, isOverOperate, isOverOperate && isPointerDown);

            // 按钮点击
            if (isPointerClicked)
            {
                if (isOverBackToMain) { OnBackToMain(); return; }
                if (isOverBack) { Close(); return; }
                if (clickedSound) { _settings.OpenSound(); return; }
                if (clickedScreen) { _settings.OpenScreen(); return; }
                if (clickedOperate) { _settings.OpenOperate(); return; }
            }
        }

        private static void UpdateButtonColor(SpriteRenderer sr, Color orig, bool isOver, bool isPressed)
        {
            if (sr == null) return;
            Color target;
            if (isPressed)      target = new Color(orig.r * 0.6f, orig.g * 0.6f, orig.b * 0.6f, orig.a);
            else if (isOver)    target = new Color(orig.r * 0.8f, orig.g * 0.8f, orig.b * 0.8f, orig.a);
            else                target = orig;
            if (sr.color != target) sr.color = target;
        }

        // ==================== Back to Main ====================

        private async void OnBackToMain()
        {
            Close();

            // 已经在主菜单 → 直接关闭
            if (_flow.CurrentConfig == null)
                return;

            // 保存存档
            _flow.GetSaveState();
            _audio?.StopBgm(0.5f);

            Debug.Log("[PauseMenu] 返回主菜单");

            // 黑屏淡入
            var fadeGo = new GameObject("ReturnFade");
            var canvas = fadeGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var img = fadeGo.AddComponent<UnityEngine.UI.Image>();
            img.color = Color.black;
            var cg = fadeGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            Object.DontDestroyOnLoad(fadeGo);
            var elapsed = 0f;
            while (elapsed < 1f) { elapsed += Time.deltaTime; cg.alpha = Mathf.Clamp01(elapsed / 1f); await System.Threading.Tasks.Task.Yield(); }
            cg.alpha = 1f;

            // 加载主菜单场景
            var handle = Addressables.LoadSceneAsync("Scenes/Start New", LoadSceneMode.Single);
            await handle.Task;
            Object.Destroy(fadeGo);
            Debug.Log("[PauseMenu] 主菜单场景加载完成");
        }

        // ==================== Util ====================

        private static Sprite CreateWhiteSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        /// <summary>生成暂停图标 "||" 的程序化 Sprite</summary>
        private static Sprite CreatePauseIcon()
        {
            var size = 64;
            var tex = new Texture2D(size, size);
            var barWidth = 14;
            var barGap = 10;
            var leftX = size / 2 - barGap - barWidth;
            var rightX = size / 2 + barGap;
            var topY = size - 12;
            var bottomY = 12;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var inLeftBar = x >= leftX && x < leftX + barWidth && y >= bottomY && y < topY;
                    var inRightBar = x >= rightX && x < rightX + barWidth && y >= bottomY && y < topY;
                    var a = (inLeftBar || inRightBar) ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
