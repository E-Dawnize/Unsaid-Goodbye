using System;
using System.Collections.Generic;
using Core.Architecture;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
using Gameplay.Interactions;
using Input;
using Input.InputInterface;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Gameplay.Puzzles
{
    public class PhotoLightPuzzleController : StrictLifecycleMonoBehaviour
    {
        [Inject] private IEventCenter _events;
        [Inject] private IPlayerInput _input;

        [Header("Defs")]
        [SerializeField] private InteractionDef _puzzleTriggerDef;
        [SerializeField] private InteractionDef _successDef;

        [Header("Layer Names")]
        [SerializeField] private string _artworkRootName = "Photo-light";
        [SerializeField] private string _photoLayerName = "photoback";
        [SerializeField] private string _lightLayerName = "light1";
        [SerializeField] private string _wordLayerName = "word";
        [SerializeField] private string _fireLayerName = "fire";
        [SerializeField] private string _targetLayerName = "fire";

        [Header("Puzzle Tuning")]
        [SerializeField] private float _panelWidthRatio = 0.67f;
        [SerializeField] private float _panelHeightRatio = 0.67f;
        [SerializeField] private float _targetRadius = 70f;
        [SerializeField] private float _revealRadius = 260f;
        [SerializeField] private float _requiredHoldTime = 0.2f;
        [SerializeField] private float _solvedClickDelay = 0.5f;
        [SerializeField] private Vector2 _targetOffset;
        [SerializeField] private Vector2 _initialLightOffsetFromTarget = new Vector2(-420f, -180f);

        private InteractableObject _interactable;
        private Collider2D[] _colliders;
        private CanvasGroup _group;
        private GameObject _canvasGo;
        private RectTransform _contentRect;
        private RectTransform _lightRect;
        private RectTransform _wordRect;
        private RectTransform _targetRect;
        private Image _wordImage;
        private bool _isOpen;
        private bool _solved;
        private bool _canCloseSolved;
        private float _holdTimer;
        private float _solvedTimer;

        protected override void OnInitialize()
        {
            _interactable = GetComponent<InteractableObject>();
            _colliders = GetComponents<Collider2D>();
        }

        protected override void OnStartExternal()
        {
            _events.Subscribe<InteractionEvent>(OnInteraction);
        }

        protected override void Tick(float deltaTime)
        {
            if (!_isOpen)
                return;

            if (!_solved && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ClosePuzzle(false);
                return;
            }

            UpdateReveal(deltaTime);

            if (_solved && _canCloseSolved && IsPrimaryClickPressed())
                ClosePuzzle(true);
        }

        protected override void OnShutdown()
        {
            _events?.Unsubscribe<InteractionEvent>(OnInteraction);
            if (_canvasGo != null)
                Destroy(_canvasGo);
            if (_input != null)
                _input.Enable();
        }

        private void OnInteraction(InteractionEvent evt)
        {
            if (_puzzleTriggerDef == null || evt.Def != _puzzleTriggerDef || _solved)
                return;

            OpenPuzzle();
        }

        private void OpenPuzzle()
        {
            if (_isOpen)
                return;

            EnsureCanvas();
            ResetPuzzleState();
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            _isOpen = true;
            _input?.Disable();
        }

        private void ClosePuzzle(bool completed)
        {
            _isOpen = false;
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = false;
                _group.interactable = false;
            }

            _input?.Enable();

            if (!completed)
                return;

            if (_interactable != null)
            {
                InteractableObject.ProximityTargets.Remove(_interactable);
                _interactable.enabled = false;
            }

            if (_colliders != null)
                foreach (var col in _colliders)
                    if (col != null) col.enabled = false;

            if (_successDef != null)
                _events.Publish(new InteractionEvent { Def = _successDef });
            else
                Debug.LogWarning("[PhotoLightPuzzle] SuccessDef is missing.", this);
        }

        private void ResetPuzzleState()
        {
            _holdTimer = 0f;
            _solvedTimer = 0f;
            _solved = false;
            _canCloseSolved = false;
            if (_lightRect != null)
                _lightRect.anchoredPosition = GetTargetPosition() + _initialLightOffsetFromTarget;
            SetWordAlpha(0f);
        }

        private void UpdateReveal(float deltaTime)
        {
            if (_lightRect == null || _wordRect == null || _wordImage == null)
                return;

            if (_solved)
            {
                SetWordAlpha(1f);
                _solvedTimer += deltaTime;
                if (_solvedTimer >= _solvedClickDelay)
                    _canCloseSolved = true;
                return;
            }

            var target = GetTargetPosition();
            var distance = Vector2.Distance(_lightRect.anchoredPosition, target);
            var reveal = Mathf.InverseLerp(_revealRadius, _targetRadius, distance);
            SetWordAlpha(Mathf.Clamp01(reveal));

            if (distance <= _targetRadius)
            {
                _holdTimer += deltaTime;
                if (_holdTimer >= _requiredHoldTime)
                {
                    _solved = true;
                    _canCloseSolved = false;
                    _solvedTimer = 0f;
                    SetWordAlpha(1f);
                }
            }
            else
            {
                _holdTimer = 0f;
            }
        }

        private void EnsureCanvas()
        {
            if (_canvasGo != null)
                return;

            EnsureEventSystem();

            _canvasGo = new GameObject("PhotoLightPuzzleCanvas");
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue - 4;

            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _canvasGo.AddComponent<GraphicRaycaster>();
            _group = _canvasGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var overlay = NewUI("Overlay", _canvasGo.transform);
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.25f);
            Stretch(overlay.GetComponent<RectTransform>());

            var panel = NewUI("PuzzlePanel", overlay.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1920f * _panelWidthRatio, 1080f * _panelHeightRatio);

            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.15f);

            _contentRect = NewUI("PhotoContent", panel.transform).GetComponent<RectTransform>();
            _contentRect.anchorMin = _contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            _contentRect.pivot = new Vector2(0.5f, 0.5f);

            BuildPhotoLayers(panelRect);
        }

        private void BuildPhotoLayers(RectTransform panelRect)
        {
            var root = ResolveArtworkRoot();
            var layers = new List<LayerData>();
            TryAddLayer(layers, root, _photoLayerName, false);
            TryAddLayer(layers, root, _fireLayerName, false);
            TryAddLayer(layers, root, _lightLayerName, true);
            TryAddLayer(layers, root, _wordLayerName, false);

            if (layers.Count == 0)
            {
                Debug.LogError("[PhotoLightPuzzle] No PSD layer sprites found.", this);
                return;
            }

            var bounds = CalculateBounds(layers);
            var baseSize = bounds.size;
            if (baseSize.x <= 0f || baseSize.y <= 0f)
                baseSize = new Vector2(1000f, 700f);

            _contentRect.sizeDelta = baseSize;
            var maxSize = panelRect.sizeDelta * 0.92f;
            var scale = Mathf.Min(maxSize.x / baseSize.x, maxSize.y / baseSize.y);
            _contentRect.localScale = Vector3.one * Mathf.Clamp(scale, 0.1f, 2f);

            foreach (var layer in layers)
                CreateLayerImage(layer, bounds.center);

            if (_wordImage == null || _lightRect == null)
                Debug.LogWarning("[PhotoLightPuzzle] Required word/light layer is missing.", this);
        }

        private void TryAddLayer(List<LayerData> layers, Transform root, string layerName, bool draggable)
        {
            var layer = FindLayer(root, layerName);
            if (layer == null)
            {
                Debug.LogWarning($"[PhotoLightPuzzle] Layer '{layerName}' not found.", this);
                return;
            }

            var sr = layer.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null)
            {
                Debug.LogWarning($"[PhotoLightPuzzle] Layer '{layerName}' has no SpriteRenderer sprite.", this);
                return;
            }

            var local = root.InverseTransformPoint(layer.position);
            layers.Add(new LayerData
            {
                Name = layerName,
                Sprite = sr.sprite,
                Position = new Vector2(local.x, local.y) * sr.sprite.pixelsPerUnit,
                Draggable = draggable
            });
        }

        private void CreateLayerImage(LayerData layer, Vector2 boundsCenter)
        {
            var go = NewUI(layer.Name, _contentRect);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = layer.Position - boundsCenter;
            rect.sizeDelta = new Vector2(layer.Sprite.rect.width, layer.Sprite.rect.height);

            var image = go.AddComponent<Image>();
            image.sprite = layer.Sprite;
            image.preserveAspect = true;
            image.raycastTarget = layer.Draggable;

            if (string.Equals(layer.Name, _wordLayerName, StringComparison.OrdinalIgnoreCase))
            {
                _wordRect = rect;
                _wordImage = image;
                SetWordAlpha(0f);
            }

            if (string.Equals(layer.Name, _targetLayerName, StringComparison.OrdinalIgnoreCase))
                _targetRect = rect;

            if (string.Equals(layer.Name, _lightLayerName, StringComparison.OrdinalIgnoreCase))
            {
                _lightRect = rect;
                var drag = go.AddComponent<DragHandle>();
                drag.Initialize(_contentRect, rect);
            }
        }

        private static Rect CalculateBounds(List<LayerData> layers)
        {
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            foreach (var layer in layers)
            {
                var size = new Vector2(layer.Sprite.rect.width, layer.Sprite.rect.height);
                min = Vector2.Min(min, layer.Position - size * 0.5f);
                max = Vector2.Max(max, layer.Position + size * 0.5f);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private Transform FindLayer(Transform root, string layerName)
        {
            if (root == null || string.IsNullOrWhiteSpace(layerName))
                return null;

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (string.Equals(child.name, layerName, StringComparison.OrdinalIgnoreCase))
                    return child;

            return null;
        }

        private Transform ResolveArtworkRoot()
        {
            var root = transform.root;
            if (FindLayer(root, _photoLayerName) != null)
                return root;

            if (!string.IsNullOrWhiteSpace(_artworkRootName))
            {
                var artworkRoot = GameObject.Find(_artworkRootName);
                if (artworkRoot != null)
                    return artworkRoot.transform;
            }

            return root;
        }

        private void SetWordAlpha(float alpha)
        {
            if (_wordImage == null)
                return;

            var color = _wordImage.color;
            color.a = alpha;
            _wordImage.color = color;
        }

        private Vector2 GetTargetPosition()
        {
            var target = _targetRect != null ? _targetRect : _wordRect;
            return target != null ? target.anchoredPosition + _targetOffset : _targetOffset;
        }

        private static bool IsPrimaryClickPressed()
        {
            return PointerInputHelper.WasClickedThisFrame;
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            DontDestroyOnLoad(go);
        }

        private struct LayerData
        {
            public string Name;
            public Sprite Sprite;
            public Vector2 Position;
            public bool Draggable;
        }

        private class DragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
        {
            private RectTransform _bounds;
            private RectTransform _target;
            private Vector2 _pointerOffset;

            public void Initialize(RectTransform bounds, RectTransform target)
            {
                _bounds = bounds;
                _target = target;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                if (_bounds == null || _target == null)
                    return;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _bounds,
                        eventData.position,
                        eventData.pressEventCamera,
                        out var local))
                {
                    _pointerOffset = _target.anchoredPosition - local;
                }
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (_bounds == null || _target == null)
                    return;

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _bounds,
                        eventData.position,
                        eventData.pressEventCamera,
                        out var local))
                {
                    return;
                }

                var half = _bounds.sizeDelta * 0.5f;
                var next = local + _pointerOffset;
                next.x = Mathf.Clamp(next.x, -half.x, half.x);
                next.y = Mathf.Clamp(next.y, -half.y, half.y);
                _target.anchoredPosition = next;
            }
        }
    }
}
