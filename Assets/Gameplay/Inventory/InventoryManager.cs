using System;
using System.Collections.Generic;
using Core.Architecture.Interfaces;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Inventory
{
    public class InventoryManager : IInventoryManager, IInitializable, IDisposable
    {
        [Inject] private IEventCenter _events;

        private readonly List<InteractionDef> _collected = new();

        private CanvasGroup _root;
        private Transform _storyColumn;
        private Transform _hiddenColumn;

        private const float ItemSize = 28f;
        private const float Spacing = 6f;
        private const float MarginRight = 24f;
        private const float MarginTop = 80f;

        private static readonly Dictionary<string, Color> ItemColors = new()
        {
            ["Def_DogCollar"]       = new Color(0.85f, 0.55f, 0.15f),
            ["Def_SofaPhoto"]       = new Color(0.95f, 0.85f, 0.40f),
            ["Def_CrateLabel"]      = new Color(0.70f, 0.50f, 0.30f),
            ["Def_WallDiary"]       = new Color(0.95f, 0.92f, 0.80f),
            ["Def_ParrotFeather"]   = new Color(0.25f, 0.75f, 0.35f),
            ["Def_PillBottle"]      = new Color(0.30f, 0.55f, 0.85f),
            ["Def_SofaPhone"]       = new Color(0.35f, 0.40f, 0.50f),
            ["Def_WallFrame"]       = new Color(0.60f, 0.40f, 0.20f),
            ["Def_BalconyCorner"]   = new Color(0.30f, 0.55f, 0.35f),
            ["Def_BedCornerSavings"]= new Color(0.85f, 0.45f, 0.55f),
        };

        private static readonly HashSet<string> StoryItemNames = new()
        {
            "Def_DogCollar", "Def_SofaPhoto", "Def_CrateLabel",
            "Def_WallDiary", "Def_ParrotFeather"
        };

        public IReadOnlyList<InteractionDef> CollectedItems => _collected;
        public event Action<InteractionDef> OnItemCollected;

        public void Initialize()
        {
            _events.Subscribe<DialogueEndedEvent>(OnDialogueEnded);
            BuildUI();
        }

        public void Dispose()
        {
            _events?.Unsubscribe<DialogueEndedEvent>(OnDialogueEnded);
            if (_root != null)
                UnityEngine.Object.Destroy(_root.gameObject);
        }

        private void OnDialogueEnded(DialogueEndedEvent e)
        {
            if (e.Def == null || _collected.Contains(e.Def)) return;
            if (!ItemColors.ContainsKey(e.Def.name)) return; // 不是道具，不收

            _collected.Add(e.Def);
            Debug.Log($"[Inventory] Item collected: {e.Def.name}");
            OnItemCollected?.Invoke(e.Def);
            RebuildUI();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("InventoryCanvas");
            UnityEngine.Object.DontDestroyOnLoad(canvasGo);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGo.AddComponent<GraphicRaycaster>();

            _root = canvasGo.AddComponent<CanvasGroup>();

            _storyColumn = BuildColumn(canvasGo.transform, "记忆道具", 0);
            _hiddenColumn = BuildColumn(canvasGo.transform, "隐藏线索", ItemSize + Spacing * 3);
        }

        private Transform BuildColumn(Transform parent, string label, float xOffset)
        {
            var root = NewUI("Column", parent);
            var rr = root.GetComponent<RectTransform>();
            rr.anchorMin = rr.anchorMax = new Vector2(1f, 1f);
            rr.pivot = new Vector2(1f, 1f);
            rr.anchoredPosition = new Vector2(-MarginRight - xOffset, -MarginTop);
            rr.sizeDelta = new Vector2(ItemSize, 0);

            var labelGo = NewUI("Label", root.transform);
            var lt = labelGo.AddComponent<Text>();
            lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lt.fontSize = 12;
            lt.alignment = TextAnchor.UpperLeft;
            lt.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            lt.text = label;
            var lr = labelGo.GetComponent<RectTransform>();
            lr.anchorMin = lr.anchorMax = new Vector2(0f, 1f);
            lr.pivot = new Vector2(0f, 1f);
            lr.anchoredPosition = new Vector2(0, 4f);
            lr.sizeDelta = new Vector2(80, 18);

            var itemsGo = NewUI("Items", root.transform);
            var ir = itemsGo.GetComponent<RectTransform>();
            ir.anchorMin = ir.anchorMax = new Vector2(0f, 1f);
            ir.pivot = new Vector2(0f, 1f);
            ir.anchoredPosition = new Vector2(0, -20f);
            ir.sizeDelta = new Vector2(ItemSize, 100);
            return itemsGo.transform;
        }

        private void RebuildUI()
        {
            foreach (Transform child in _storyColumn) UnityEngine.Object.Destroy(child.gameObject);
            foreach (Transform child in _hiddenColumn) UnityEngine.Object.Destroy(child.gameObject);

            var si = 0;
            var hi = 0;

            foreach (var item in _collected)
            {
                var isStory = StoryItemNames.Contains(item.name);
                var column = isStory ? _storyColumn : _hiddenColumn;
                var idx = isStory ? si++ : hi++;
                var color = ItemColors.TryGetValue(item.name, out var c) ? c : Color.gray;

                var go = NewUI(item.name, column);
                var img = go.AddComponent<Image>();
                img.color = color;

                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(0, -(idx * (ItemSize + Spacing)));
                rect.sizeDelta = new Vector2(ItemSize, ItemSize);
            }
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
