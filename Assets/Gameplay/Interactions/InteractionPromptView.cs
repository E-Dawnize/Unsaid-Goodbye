using System;
using System.Collections.Generic;
using Core.Architecture;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Proximity 模式交互提示 UI — 玩家靠近交互物时在右侧显示可点击图标。
    /// 不同交互物通过 InteractionDef.PromptIconKey 指定不同图标的 Addressables Key。
    /// 点击图标即触发 InteractableObject.Fire()。
    /// </summary>
    public class InteractionPromptView : StrictLifecycleMonoBehaviour
    {
        private const string PopupLayer = "Popup";
        private const float PlayerOffsetX = 1.3f;

        private Camera _cam;
        private Transform _playerTransform;

        private GameObject _promptGo;
        private SpriteRenderer _sr;
        private BoxCollider2D _col;

        private string _loadedIconKey;
        private AsyncOperationHandle<Sprite> _iconLoadHandle;
        private bool _visible;
        private bool _usingDefaultSprite;
        private InteractableObject _currentTarget;
        private string _lastWarnedDefName;

        // 默认图标（未配置 PromptIconKey 时使用）
        private static Sprite _defaultPromptSprite;

        protected override void OnInitialize()
        {
            DontDestroyOnLoad(gameObject);
            _cam = Camera.main;
        }

        protected override void OnStartExternal()
        {
            BuildPromptUI();
        }

        private void BuildPromptUI()
        {
            _promptGo = new GameObject("InteractionPromptIcon");
            DontDestroyOnLoad(_promptGo);

            _sr = _promptGo.AddComponent<SpriteRenderer>();
            _sr.sortingLayerName = PopupLayer;
            _sr.sortingOrder = 50;
            _sr.enabled = false;

            _col = _promptGo.AddComponent<BoxCollider2D>();
            _col.isTrigger = true;
            _col.size = Vector2.one;

            _promptGo.layer = LayerMask.NameToLayer("UI");
        }

        protected override void Tick(float dt)
        {
            RefreshCamera();

            var target = InteractableObject.CurrentProximityTarget;

            if (target != _currentTarget)
            {
                _currentTarget = target;
                _usingDefaultSprite = false;

                if (target != null)
                {
                    var iconKey = target.Def?.PromptIconKey;
                    if (!string.IsNullOrEmpty(iconKey))
                    {
                        if (iconKey != _loadedIconKey)
                            LoadIcon(iconKey);
                    }
                    else
                    {
                        // 未配置 PromptIconKey → 使用默认图标并警告
                        ApplyDefaultSprite();
                        var defName = target.Def != null ? target.Def.name : "(null)";
                        if (defName != _lastWarnedDefName)//每个def只警告一次
                        {
                            _lastWarnedDefName = defName;
                            Debug.LogWarning($"[InteractionPrompt] InteractionDef '{defName}' 未配置 PromptIconKey，使用默认图标");
                        }
                    }
                }
            }

            if (target != null && target.CanInteract)
            {
                UpdatePlayerRef();
                UpdatePosition();

                if (!_visible)
                {
                    _visible = true;
                    if (_sr != null) _sr.enabled = true;
                }

                CheckClick();
            }
            else if (_visible)
            {
                _visible = false;
                if (_sr != null) _sr.enabled = false;
            }
        }

        private void RefreshCamera()
        {
            // 场景切换后 Camera.main 会变，每帧刷新
            if (_cam == null || !_cam.gameObject.activeInHierarchy)
                _cam = Camera.main;
        }

        private void UpdatePlayerRef()
        {
            if (_playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) _playerTransform = player.transform;
            }
        }

        private void UpdatePosition()
        {
            if (_playerTransform == null || _cam == null) return;
            var p = _playerTransform.position;
            _promptGo.transform.position = new Vector3(p.x + PlayerOffsetX, p.y, p.z);
        }

        private void CheckClick()
        {
            var mouse = Mouse.current;
            if (mouse == null || _cam == null || _currentTarget == null) return;
            if (!mouse.leftButton.wasPressedThisFrame) return;

            var worldPos = (Vector2)_cam.ScreenToWorldPoint(mouse.position.ReadValue());
            var hits = new List<Collider2D>();
            Physics2D.OverlapPoint(worldPos, new ContactFilter2D().NoFilter(), hits);

            foreach (var hit in hits)
            {
                if (hit.gameObject == _promptGo)
                {
                    _currentTarget.Fire();
                    return;
                }
            }
        }

        private void ApplyDefaultSprite()
        {
            _loadedIconKey = null;
            _usingDefaultSprite = true;

            if (_iconLoadHandle.IsValid())
                Addressables.Release(_iconLoadHandle);

            if (_sr != null)
            {
                _sr.sprite = GetDefaultPromptSprite();
                _col.size = Vector2.one * 1.2f;
            }
        }

        private static Sprite GetDefaultPromptSprite()
        {
            if (_defaultPromptSprite != null) return _defaultPromptSprite;

            var tex = new Texture2D(64, 64);
            var cx = 32f;
            var cy = 32f;
            var r = 28f;
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    var dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    var alpha = 1f - Mathf.Clamp01((dist - r + 2f) / 4f);
                    var edge = Mathf.Clamp01((r - dist) * 10f);
                    var a = alpha * (0.25f + edge * 0.75f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            _defaultPromptSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            return _defaultPromptSprite;
        }

        private async void LoadIcon(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (_iconLoadHandle.IsValid())
                Addressables.Release(_iconLoadHandle);

            _loadedIconKey = key;
            _usingDefaultSprite = false;

            try
            {
                _iconLoadHandle = Addressables.LoadAssetAsync<Sprite>(key);
                await _iconLoadHandle.Task;

                if (_iconLoadHandle.Status == AsyncOperationStatus.Succeeded && _sr != null)
                {
                    _sr.sprite = _iconLoadHandle.Result;
                    if (_sr.sprite != null)
                        _col.size = _sr.sprite.bounds.size;
                }
                else
                {
                    Debug.LogWarning($"[InteractionPrompt] Failed to load icon: {key}, using default");
                    ApplyDefaultSprite();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InteractionPrompt] Error loading icon '{key}': {ex.Message}, using default");
                ApplyDefaultSprite();
            }
        }

        protected override void OnShutdown()
        {
            if (_promptGo != null) Destroy(_promptGo);
            if (_iconLoadHandle.IsValid()) Addressables.Release(_iconLoadHandle);
        }
    }
}
