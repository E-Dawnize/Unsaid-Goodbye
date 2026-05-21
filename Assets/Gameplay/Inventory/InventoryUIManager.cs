using System;
using System.Collections.Generic;
using Core.Architecture.Interfaces;
using Core.DI;
using Core.Identity;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Gameplay.Inventory
{
    public class InventoryUIManager : IInitializable, IDisposable, IBackpackUI
    {
        [Inject] private IInventoryManager _inventory;

        public bool IsOpen { get; private set; }

        private const string PrefabKey = "Arts/UI/Bag";
        private const string B1Key = "Arts/UI/UIinterface[B1]";
        private const string B2Key = "Arts/UI/UIinterface[B2]";
        private const string C1Key = "Arts/UI/UIinterface[C1]";
        private const string C2Key = "Arts/UI/UIinterface[C2]";
        private const string PopupLayer = "Popup";

        private static readonly Dictionary<string, string> DefToSlot = new()
        {
            ["Def_DogCollar"]       = "L1",
            ["Def_SofaPhoto"]       = "L2",
            ["Def_CrateLabel"]      = "L3",
            ["Def_WallDiary"]       = "L4",
            ["Def_ParrotFeather"]   = "L5",
            ["Def_PillBottle"]      = "H0",
            ["Def_SofaPhone"]       = "H1",
            ["Def_WallFrame"]       = "H2",
            ["Def_BalconyCorner"]   = "H3",
            ["Def_BedCornerSavings"]= "H4",
        };

        private static readonly Dictionary<string, string> DetailKeys = new()
        {
            ["Def_DogCollar"]       = "Arts/open-items/open-l0",
            ["Def_SofaPhoto"]       = "Arts/open-items/open-l1",
            ["Def_CrateLabel"]      = "Arts/open-items/open-l2",
            ["Def_WallDiary"]       = "Arts/open-items/open-l3",
            ["Def_ParrotFeather"]   = "Arts/open-items/open-l4",
            ["Def_PillBottle"]      = "Arts/open-items/open-h0",
            ["Def_SofaPhone"]       = "Arts/open-items/open-h1",
            ["Def_WallFrame"]       = "Arts/open-items/open-h2",
            ["Def_BalconyCorner"]   = "Arts/open-items/open-h3",
            ["Def_BedCornerSavings"]= "Arts/open-items/open-h4",
        };

        // prefab
        private GameObject _backpackRoot;
        private readonly Dictionary<string, SpriteRenderer> _slotRenderers = new();
        private readonly Dictionary<string, string> _slotDefs = new();
        private AsyncOperationHandle<GameObject> _loadHandle;

        // world-space overlay + buttons
        private GameObject _overlayGo;
        private WorldButton _toggleBtn;
        private WorldButton _closeBtn;
        private Camera _cam;

        // detail (Canvas overlay, renders above world-space sprites)
        private GameObject _detailCanvasGo;
        private CanvasGroup _detailOverlay;
        private Image _detailImage;
        private Transform _detailImageTransform;
        private readonly Dictionary<string, Sprite> _loadedDetailSprites = new();

        // ui sprites
        private Sprite _b1, _b2, _c1, _c2;

        public void Initialize()
        {
            _cam = Camera.main;
            _inventory.OnItemCollected += OnItemCollected;
            BuildOverlay();
            BuildToggleBtn();
            BuildDetailCanvas();
            LoadUISprites();
            LoadPrefab();
        }

        public void Dispose()
        {
            _inventory.OnItemCollected -= OnItemCollected;
            if (_backpackRoot != null) Object.Destroy(_backpackRoot);
            if (_overlayGo != null) Object.Destroy(_overlayGo);
            if (_toggleBtn != null) Object.Destroy(_toggleBtn.gameObject);
            if (_closeBtn != null) Object.Destroy(_closeBtn.gameObject);
            if (_detailCanvasGo != null) Object.Destroy(_detailCanvasGo);
            if (_loadHandle.IsValid()) Addressables.Release(_loadHandle);
        }

        // ==================== world-space overlay ====================

        private void BuildOverlay()
        {
            _overlayGo = new GameObject("BackpackOverlay");
            Object.DontDestroyOnLoad(_overlayGo);

            var camZ = _cam != null ? _cam.transform.position.z : -10f;
            _overlayGo.transform.position = new Vector3(0f, 0f, camZ + 5f);

            var sr = _overlayGo.AddComponent<SpriteRenderer>();
            sr.sprite = CreateWhiteSprite();
            sr.color = new Color(0.02f, 0.01f, 0.04f, 0.55f);
            sr.sortingLayerName = PopupLayer;
            sr.sortingOrder = 0;

            if (_cam != null)
            {
                var halfH = _cam.orthographicSize;
                var halfW = halfH * _cam.aspect;
                _overlayGo.transform.localScale = new Vector3(halfW * 2f, halfH * 2f, 1f);
            }

            _overlayGo.SetActive(false);
        }

        private static Sprite CreateWhiteSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        // ==================== world-space buttons ====================

        private void BuildToggleBtn()
        {
            var go = CreateCameraChild("BackpackToggleBtn");
            var hh = _cam.orthographicSize;
            var hw = hh * _cam.aspect;
            go.transform.localPosition = new Vector3(hw - 0.7f, -hh + 0.7f, 5f);

            _toggleBtn = go.AddComponent<WorldButton>();
            _toggleBtn.Init(PopupLayer, 100, new Vector2(1f, 1f));
            _toggleBtn.OnClick += Toggle;
            _toggleBtn.gameObject.SetActive(true);
        }

        private void CreateCloseBtnOnBackpack()
        {
            var go = CreateCameraChild("BackpackCloseBtn");
            var hh = _cam.orthographicSize;
            var hw = hh * _cam.aspect;
            go.transform.localPosition = new Vector3(hw - 2.45f, hh - 2.01f, 5f);

            _closeBtn = go.AddComponent<WorldButton>();
            _closeBtn.Init(PopupLayer, 110, new Vector2(0.4f, 0.4f));
            _closeBtn.OnClick += Close;
            _closeBtn.gameObject.SetActive(false);
        }

        private static GameObject CreateCameraChild(string name)
        {
            var go = new GameObject(name);
            var cam = Camera.main;
            if (cam != null) go.transform.SetParent(cam.transform, false);
            Object.DontDestroyOnLoad(go);
            return go;
        }

        // ==================== detail canvas ====================

        private void BuildDetailCanvas()
        {
            _detailCanvasGo = new GameObject("DetailCanvas");
            Object.DontDestroyOnLoad(_detailCanvasGo);

            var canvas = _detailCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue - 1;

            var scaler = _detailCanvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _detailCanvasGo.AddComponent<GraphicRaycaster>();

            var go = NewUI("DetailOverlay", _detailCanvasGo.transform);
            _detailOverlay = go.AddComponent<CanvasGroup>();
            _detailOverlay.alpha = 0f;
            _detailOverlay.blocksRaycasts = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            var bgBtn = go.AddComponent<Button>();
            bgBtn.onClick.AddListener(HideDetail);

            var detailGo = NewUI("DetailImage", go.transform);
            _detailImage = detailGo.AddComponent<Image>();
            _detailImage.preserveAspect = true;
            _detailImageTransform = detailGo.transform;
            var dr = detailGo.GetComponent<RectTransform>();
            dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0.5f);
            dr.pivot = new Vector2(0.5f, 0.5f);
            dr.sizeDelta = new Vector2(800f, 800f);

            EnsureEventSystem();
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) return;
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Object.DontDestroyOnLoad(esGo);
        }

        // ==================== ui sprites ====================

        private async void LoadUISprites()
        {
            var tasks = new[]
            {
                LoadSprite(B1Key, s => _b1 = s),
                LoadSprite(B2Key, s => _b2 = s),
                LoadSprite(C1Key, s => _c1 = s),
                LoadSprite(C2Key, s => _c2 = s),
            };
            await System.Threading.Tasks.Task.WhenAll(tasks);
            if (_b1 == null) Debug.LogWarning($"[InventoryUI] Failed to load B1: {B1Key}");
            if (_b2 == null) Debug.LogWarning($"[InventoryUI] Failed to load B2: {B2Key}");
            if (_c1 == null) Debug.LogWarning($"[InventoryUI] Failed to load C1: {C1Key}");
            if (_c2 == null) Debug.LogWarning($"[InventoryUI] Failed to load C2: {C2Key}");
            ApplySprites();
        }

        private async System.Threading.Tasks.Task LoadSprite(string key, Action<Sprite> setter)
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<Sprite>(key);
                await handle.Task;
                if (handle.Status == AsyncOperationStatus.Succeeded)
                    setter(handle.Result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventoryUI] Error loading sprite '{key}': {ex.Message}");
            }
        }

        private void ApplySprites()
        {
            if (_toggleBtn != null && _b1 != null) _toggleBtn.SetSprites(_b1, _b2);
            if (_closeBtn != null && _c1 != null) _closeBtn.SetSprites(_c1, _c2);
        }

        // ==================== prefab ====================

        private async void LoadPrefab()
        {
            try
            {
                _loadHandle = Addressables.InstantiateAsync(PrefabKey);
                await _loadHandle.Task;

                if (_loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[InventoryUI] Failed to load prefab: {PrefabKey}");
                    return;
                }

                _backpackRoot = _loadHandle.Result;
                _backpackRoot.SetActive(false);
                Object.DontDestroyOnLoad(_backpackRoot);

                // 移除多余的 back 子物体
                var backChild = _backpackRoot.transform.Find("back");
                if (backChild != null) Object.Destroy(backChild.gameObject);

                var baseOrder = 1;
                foreach (var sr in _backpackRoot.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.sortingLayerName = PopupLayer;
                    sr.sortingOrder = baseOrder + sr.sortingOrder;
                }

                foreach (var kv in DefToSlot)
                {
                    var child = _backpackRoot.transform.Find(kv.Value);
                    if (child == null)
                    {
                        Debug.LogWarning($"[InventoryUI] Slot '{kv.Value}' not found in prefab");
                        continue;
                    }
                    var sr = child.GetComponent<SpriteRenderer>();
                    if (sr == null)
                    {
                        Debug.LogWarning($"[InventoryUI] No SpriteRenderer on slot '{kv.Value}'");
                        continue;
                    }
                    sr.enabled = false;
                    _slotRenderers[kv.Value] = sr;
                    _slotDefs[kv.Value] = kv.Key;

                    var col = child.gameObject.AddComponent<BoxCollider2D>();
                    col.isTrigger = true;
                    col.size = sr.bounds.size;
                    var slotName = kv.Value;
                    child.gameObject.AddComponent<SlotClickHandler>().OnClick += () => ShowDetail(slotName);
                }

                foreach (var def in _inventory.CollectedItems)
                    ShowItem(def);

                CreateCloseBtnOnBackpack();
                // sprite 可能已经加载好了，直接设
                if (_closeBtn != null && _c1 != null) _closeBtn.SetSprites(_c1, _c2);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventoryUI] Error loading prefab: {ex.Message}");
            }
        }

        // ==================== items ====================

        private void OnItemCollected(InteractionDef def)
        {
            ShowItem(def);

            if (DefToSlot.TryGetValue(def.name, out var slotName) &&
                _slotRenderers.TryGetValue(slotName, out var spr))
            {
                spr.transform.parent.DOPunchScale(Vector3.one * 0.35f, 0.4f, 2, 0.5f);
            }
        }

        private void ShowItem(InteractionDef def)
        {
            if (!DefToSlot.TryGetValue(def.name, out var slotName)) return;
            if (!_slotRenderers.TryGetValue(slotName, out var sr)) return;
            sr.enabled = true;
        }

        // ==================== detail ====================

        private async void ShowDetail(string slotName)
        {
            if (!_slotDefs.TryGetValue(slotName, out var defName)) return;
            if (!_slotRenderers.TryGetValue(slotName, out var sr) || !sr.enabled) return;

            if (!_loadedDetailSprites.TryGetValue(defName, out var sprite))
            {
                if (DetailKeys.TryGetValue(defName, out var key))
                {
                    var handle = Addressables.LoadAssetAsync<Sprite>(key);
                    await handle.Task;
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        sprite = handle.Result;
                        _loadedDetailSprites[defName] = sprite;
                    }
                }
            }

            if (sprite != null)
            {
                _detailImage.sprite = sprite;
                _detailImage.color = Color.white;
            }
            else
            {
                _detailImage.sprite = null;
                _detailImage.color = Color.gray;
            }

            _detailOverlay.blocksRaycasts = true;
            FadeCG(_detailOverlay, 1f, 0.25f);
            _detailImageTransform.localScale = Vector3.one * 0.7f;
            _detailImageTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        private void HideDetail()
        {
            _detailOverlay.blocksRaycasts = false;
            FadeCG(_detailOverlay, 0f, 0.2f);
            _detailImageTransform.DOScale(0.7f, 0.15f);
        }

        // ==================== open / close ====================

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (_backpackRoot == null) return;
            IsOpen = true;
            _overlayGo.SetActive(true);
            _backpackRoot.SetActive(true);
            _backpackRoot.transform.localScale = Vector3.one * 0.6f;
            _backpackRoot.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
            if (_closeBtn != null) _closeBtn.gameObject.SetActive(true);
        }

        public void Close()
        {
            IsOpen = false;
            HideDetail();
            if (_closeBtn != null) _closeBtn.gameObject.SetActive(false);
            if (_overlayGo != null) _overlayGo.SetActive(false);
            if (_backpackRoot != null)
            {
                _backpackRoot.transform.DOScale(0.6f, 0.25f).SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        if (!IsOpen)
                            _backpackRoot.SetActive(false);
                    });
            }
        }

        // ==================== util ====================

        private static void FadeCG(CanvasGroup cg, float target, float duration)
        {
            DOTween.Kill(cg);
            DOTween.To(() => cg.alpha, v => cg.alpha = v, target, duration).SetTarget(cg);
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        // ==================== world-space button helper ====================

        private class WorldButton : MonoBehaviour
        {
            public event Action OnClick;

            private SpriteRenderer _sr;
            private BoxCollider2D _col;
            private Sprite _normal, _hover;
            private bool _hovered;

            public void Init(string sortingLayer, int sortingOrder, Vector2 defaultSize)
            {
                _sr = gameObject.AddComponent<SpriteRenderer>();
                _sr.sortingLayerName = sortingLayer;
                _sr.sortingOrder = sortingOrder;
                _sr.sprite = CreatePlaceholder();
                _sr.color = new Color(1f, 1f, 1f, 0.6f);

                _col = gameObject.AddComponent<BoxCollider2D>();
                _col.isTrigger = true;
                _col.size = defaultSize;

                gameObject.layer = LayerMask.NameToLayer("UI");
            }

            public void SetSprites(Sprite normal, Sprite hover)
            {
                _normal = normal;
                _hover = hover;
                if (normal != null)
                {
                    _sr.sprite = normal;
                    _sr.color = Color.white;
                    _col.size = normal.bounds.size;
                }
            }

            private static Sprite CreatePlaceholder()
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            }

            private void Update()
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse == null) return;

                var cam = Camera.main;
                if (cam == null) return;

                var worldPos = (Vector2)cam.ScreenToWorldPoint(mouse.position.ReadValue());
                var hits = new List<Collider2D>();
                Physics2D.OverlapPoint(worldPos, new ContactFilter2D().NoFilter(), hits);

                var wasHovered = _hovered;
                _hovered = false;
                foreach (var hit in hits)
                {
                    if (hit.gameObject == gameObject)
                    {
                        _hovered = true;
                        break;
                    }
                }

                if (_hovered && !wasHovered)
                    _sr.sprite = _hover ?? _normal ?? _sr.sprite;
                else if (!_hovered && wasHovered)
                    _sr.sprite = _normal ?? _sr.sprite;

                if (_hovered && mouse.leftButton.wasPressedThisFrame)
                    OnClick?.Invoke();
            }
        }

        // ==================== slot click helper ====================

        private class SlotClickHandler : MonoBehaviour
        {
            public event Action OnClick;
            private Camera _cam;
            private Transform _target;
            private bool _hovered;
            private Vector3 _originalScale;

            private void Start()
            {
                _cam = Camera.main;
                _target = transform;
                _originalScale = _target.localScale;
            }

            private void Update()
            {
                if (_cam == null) return;
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse == null) return;

                var worldPos = (Vector2)_cam.ScreenToWorldPoint(mouse.position.ReadValue());
                var hits = new List<Collider2D>();
                Physics2D.OverlapPoint(worldPos, new ContactFilter2D().NoFilter(), hits);

                var isOver = false;
                foreach (var hit in hits)
                {
                    if (hit.gameObject == gameObject)
                    {
                        isOver = true;
                        break;
                    }
                }

                if (isOver && !_hovered)
                {
                    _hovered = true;
                    _target.DOScale(_originalScale * 1.15f, 0.15f).SetEase(Ease.OutQuad);
                }
                else if (!isOver && _hovered)
                {
                    _hovered = false;
                    _target.DOScale(_originalScale, 0.15f).SetEase(Ease.OutQuad);
                }

                if (_hovered && mouse.leftButton.wasPressedThisFrame)
                    OnClick?.Invoke();
            }
        }
    }
}
