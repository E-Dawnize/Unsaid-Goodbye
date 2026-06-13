using System;
using System.Collections.Generic;
using Core.Architecture;
using UnityEngine;
using Input;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gameplay.Interactions
{
    public class InteractionPromptView : StrictLifecycleMonoBehaviour
    {
        private const string PopupLayer = "Popup";
        private const float PlayerOffsetX = 1.5f;
        private const float StackSpacing = 1.2f;

        private Camera _cam;
        private Transform _playerTransform;

        private readonly Dictionary<InteractableObject, PromptEntry> _entries = new();
        private readonly List<InteractableObject> _toRemove = new();
        private static Sprite _defaultPromptSprite;
        private readonly HashSet<string> _warnedDefs = new();

        protected override void OnInitialize()
        {
            DontDestroyOnLoad(gameObject);
            _cam = Camera.main;
        }

        protected override void Tick(float dt)
        {
            RefreshCamera();
            UpdatePlayerRef();

            var targets = InteractableObject.ProximityTargets;

            // 移除已离开范围的条目
            _toRemove.Clear();
            foreach (var kv in _entries)
                if (!targets.Contains(kv.Key))
                    _toRemove.Add(kv.Key);
            foreach (var t in _toRemove)
                RemoveEntry(t);

            // 添加新进入范围的条目
            foreach (var target in targets)
            {
                if (!_entries.ContainsKey(target))
                    AddEntry(target);
            }

            // 更新位置和点击检测
            if (_entries.Count == 0 || _playerTransform == null) return;

            // 以玩家正右方为基准，上下排开居中
            var basePos = _playerTransform.position;
            var count = _entries.Count;
            var totalH = (count - 1) * StackSpacing;
            var startY = basePos.y + totalH * 0.5f;
            var idx = 0;
            foreach (var kv in _entries)
            {
                var pos = new Vector3(basePos.x + PlayerOffsetX, startY - idx * StackSpacing, basePos.z);
                kv.Value.GameObject.transform.position = pos;
                idx++;
            }

            CheckClicks();
        }

        private void AddEntry(InteractableObject target)
        {
            var go = new GameObject("Prompt_" + (target.Def != null ? target.Def.name : "null"));
            go.SetActive(false);
            DontDestroyOnLoad(go);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = PopupLayer;
            sr.sortingOrder = 50;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;
            go.layer = LayerMask.NameToLayer("UI");

            var entry = new PromptEntry { GameObject = go, Sprite = sr, Collider = col, Target = target };
            _entries[target] = entry;

            // 加载图标
            var iconKey = target.Def?.PromptIconKey;
            if (!string.IsNullOrEmpty(iconKey))
            {
                LoadIcon(entry, iconKey);
            }
            else
            {
                ApplyDefaultSprite(entry);
                var defName = target.Def != null ? target.Def.name : "(null)";
                if (_warnedDefs.Add(defName))
                    Debug.LogWarning($"[InteractionPrompt] InteractionDef '{defName}' 未配置 PromptIconKey，使用默认图标");
            }

            go.SetActive(true);
        }

        private void RemoveEntry(InteractableObject target)
        {
            if (_entries.TryGetValue(target, out var entry))
            {
                if (entry.IconHandle.IsValid()) Addressables.Release(entry.IconHandle);
                if (entry.GameObject != null) Destroy(entry.GameObject);
                _entries.Remove(target);
            }
        }

        private void CheckClicks()
        {
            if (!PointerInputHelper.WasClickedThisFrame) return;
            if (_cam == null) return;

            var pointerPos = PointerInputHelper.ScreenPosition;
            var worldPos = (Vector2)_cam.ScreenToWorldPoint(pointerPos);
            var hits = new List<Collider2D>();
            Physics2D.OverlapPoint(worldPos, new ContactFilter2D().NoFilter(), hits);

            foreach (var hit in hits)
            {
                foreach (var kv in _entries)
                {
                    if (hit.gameObject == kv.Value.GameObject)
                    {
                        kv.Key.Fire();
                        return;
                    }
                }
            }
        }

        // ==================== Icon loading ====================

        private async void LoadIcon(PromptEntry entry, string key)
        {
            try
            {
                entry.IconHandle = Addressables.LoadAssetAsync<Sprite>(key);
                await entry.IconHandle.Task;
                if (entry.IconHandle.Status == AsyncOperationStatus.Succeeded && entry.Sprite != null)
                {
                    entry.Sprite.sprite = entry.IconHandle.Result;
                    if (entry.Sprite.sprite != null)
                        entry.Collider.size = entry.Sprite.sprite.bounds.size;
                }
                else
                {
                    ApplyDefaultSprite(entry);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InteractionPrompt] Error loading icon '{key}': {ex.Message}");
                ApplyDefaultSprite(entry);
            }
        }

        private static void ApplyDefaultSprite(PromptEntry entry)
        {
            if (entry.Sprite != null)
            {
                entry.Sprite.sprite = GetDefaultPromptSprite();
                entry.Collider.size = Vector2.one * 1.2f;
            }
        }

        private static Sprite GetDefaultPromptSprite()
        {
            if (_defaultPromptSprite != null) return _defaultPromptSprite;
            var w = 96; var h = 48;
            var tex = new Texture2D(w, h);
            var cx = w * 0.5f; var cy = h * 0.5f;
            var rx = 44f; var ry = 20f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var dx = (x - cx) / rx; var dy = (y - cy) / ry;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = 1f - Mathf.Clamp01((d - 0.9f) / 0.15f);
                    var edge = Mathf.Clamp01((1f - d) * 10f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * (0.25f + edge * 0.75f)));
                }
            tex.Apply();
            _defaultPromptSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
            return _defaultPromptSprite;
        }

        // ==================== Helpers ====================

        private void RefreshCamera()
        {
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

        protected override void OnShutdown()
        {
            foreach (var kv in _entries)
            {
                if (kv.Value.IconHandle.IsValid()) Addressables.Release(kv.Value.IconHandle);
                if (kv.Value.GameObject != null) Destroy(kv.Value.GameObject);
            }
            _entries.Clear();
        }

        private class PromptEntry
        {
            public GameObject GameObject;
            public SpriteRenderer Sprite;
            public BoxCollider2D Collider;
            public InteractableObject Target;
            public AsyncOperationHandle<Sprite> IconHandle;
        }
    }
}
