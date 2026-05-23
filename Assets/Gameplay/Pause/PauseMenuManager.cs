using System;
using System.Collections.Generic;
using Core.Architecture.Interfaces;
using Core.DI;
using Gameplay.Interfaces;
using Gameplay.Save;
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

        public bool IsOpen { get; private set; }

        /// <summary>暂停菜单打开时屏蔽背包等底层 UI 的点击</summary>
        public static bool IsAnyOpen { get; private set; }

        private const string PrefabKey = "Arts/UI/Settings";
        private const string PopupLayer = "Popup";
        private const float ToggleOffsetX = 0.7f;
        private const float ToggleOffsetY = 0.7f;

        private Camera _cam;

        // Toggle button
        private GameObject _toggleGo;
        private SpriteRenderer _toggleSr;
        private BoxCollider2D _toggleCol;
        private bool _toggleHovered;

        // Overlay
        private GameObject _overlayGo;

        // Prefab
        private GameObject _menuRoot;
        private AsyncOperationHandle<GameObject> _prefabHandle;
        private GameObject _backToMainGo;
        private GameObject _backGo;
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
            if (_prefabHandle.IsValid()) Addressables.Release(_prefabHandle);
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

            _overlayGo.SetActive(false);
        }

        // ==================== Toggle Button ====================

        private void BuildToggleBtn()
        {
            _toggleGo = new GameObject("PauseToggleBtn");
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

            UpdateTogglePosition();
            _toggleGo.SetActive(false);
        }

        private void UpdateTogglePosition()
        {
            if (_cam == null || _toggleGo == null) return;
            var hh = _cam.orthographicSize;
            var hw = hh * _cam.aspect;
            _toggleGo.transform.position = _cam.transform.position + new Vector3(hw - ToggleOffsetX, hh - ToggleOffsetY, 5f);
        }

        // ==================== Prefab ====================

        private async void LoadPrefab()
        {
            try
            {
                _prefabHandle = Addressables.InstantiateAsync(PrefabKey);
                await _prefabHandle.Task;

                if (_prefabHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[PauseMenu] Failed to load prefab: {PrefabKey}");
                    return;
                }

                _menuRoot = _prefabHandle.Result;
                _menuRoot.SetActive(false);
                Object.DontDestroyOnLoad(_menuRoot);

                // 设置所有 SpriteRenderer 的 sorting layer（基准 106，高于背包的 100）
                foreach (var sr in _menuRoot.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.sortingLayerName = PopupLayer;
                    sr.sortingOrder = 106 + sr.sortingOrder;
                }

                // 找到按钮子物体（名称以 Prefab 中实际命名为准）
                _backToMainGo = FindChildRecursive(_menuRoot.transform, "从选区 图像_5");
                _backGo = FindChildRecursive(_menuRoot.transform, "从选区 图像");

                if (_backToMainGo == null)
                    Debug.LogWarning($"[PauseMenu] Child 'backtomain' not found in prefab {PrefabKey}");
                else
                    SetupClickTarget(_backToMainGo);

                if (_backGo == null)
                    Debug.LogWarning($"[PauseMenu] Child 'back' not found in prefab {PrefabKey}");
                else
                    SetupClickTarget(_backGo);
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
            if (mouse == null) return;

            var worldPos = (Vector2)_cam.ScreenToWorldPoint(mouse.position.ReadValue());
            var hits = new List<Collider2D>();
            Physics2D.OverlapPoint(worldPos, new ContactFilter2D().NoFilter(), hits);

            // Toggle button hover + click
            var isOverToggle = false;
            foreach (var hit in hits)
            {
                if (hit.gameObject == _toggleGo)
                {
                    isOverToggle = true;
                    if (mouse.leftButton.wasPressedThisFrame)
                    {
                        Toggle();
                        return;
                    }
                }
            }

            if (isOverToggle != _toggleHovered)
            {
                _toggleHovered = isOverToggle;
                _toggleSr.color = isOverToggle
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(1f, 1f, 1f, 0.7f);
            }

            if (!IsOpen) return;

            // Menu 内按钮点击
            foreach (var hit in hits)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (_backToMainGo != null && hit.gameObject == _backToMainGo)
                    {
                        OnBackToMain();
                        return;
                    }
                    if (_backGo != null && hit.gameObject == _backGo)
                    {
                        Close();
                        return;
                    }
                }
            }
        }

        // ==================== Back to Main ====================

        private async void OnBackToMain()
        {
            Close();

            // 保存存档
            if (_flow.CurrentConfig != null)
                _flow.GetSaveState();

            Debug.Log("[PauseMenu] 返回主菜单");

            // Addressables 加载主菜单场景
            const string mainMenuScene = "Scenes/Start New";
            var handle = Addressables.LoadSceneAsync(mainMenuScene, LoadSceneMode.Single);
            await handle.Task;
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
