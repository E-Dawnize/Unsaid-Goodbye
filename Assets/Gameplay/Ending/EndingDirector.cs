using System;
using System.Collections.Generic;
using Core.Architecture;
using Core.DI;
using Core.Events.EventInterfaces;
using DG.Tweening;
using Gameplay.Dialogue;
using Gameplay.Interfaces;
using Gameplay.SceneFlow;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace Gameplay.Ending
{
    /// <summary>
    /// 全代码结局编排——不依赖 .unity 场景文件，用 Addressables 图片+对话+视频组成结局演出。
    /// </summary>
    public class EndingDirector : StrictLifecycleMonoBehaviour
    {
        [Inject] private IGameFlowManager _flow;
        [Inject] private IDialogueManager _dialogue;
        [Inject] private IEventCenter _events;

        private const string PopupLayer = "Popup";
        private const string MasterKey = "Ending/Master";
        private const string EndingAKey = "Arts/Ending/Ending-A";
        private const string AfterABKey = "Arts/Ending/AfterAB";
        private const string CharacterKey = "Arts/Ending/character-moon";

        /// <summary>动画进行中禁止移动/对话/切图</summary>
        public static bool IsAnimating { get; private set; }
        private float _cooldownUntil;
        private bool _masterSpawned;

        private static readonly string[] EndingBKeys =
        {
            "Arts/Ending/B/end01", "Arts/Ending/B/end02", "Arts/Ending/B/end03",
            "Arts/Ending/B/end04", "Arts/Ending/B/end05", "Arts/Ending/B/end06",
            "Arts/Ending/B/end07"
        };

        // UI
        private GameObject _overlayGo;
        private SpriteRenderer _bgSr;
        private GameObject _videoGo;
        private VideoPlayer _videoPlayer;
        private RenderTexture _videoRT;
        private VideoClip _videoClip;
        private GameObject _videoQuad;

        // State
        private bool _active;
        private GamePhase _endingType;
        private int _imageIndex;
        private bool _dialogueFinished;
        private bool _showingAfter;
        private readonly List<Sprite> _bSprites = new();
        private Sprite _afterABSprite;
        private Sprite _characterSprite;
        private bool _spritePreloaded;

        // Choice UI
        private GameObject _stayBtn;
        private GameObject _leaveBtn;
        private GameObject _choiceLabelGo;
        private bool _choiceActive;

        protected override void OnInitialize()
        {
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        protected override void OnStartExternal()
        {
            Debug.Log($"[EndingDirector] OnStartExternal");
            _flow.OnPhaseChanged += OnPhaseChanged;
            _events?.Subscribe<DialogueEndedEvent>(OnDialogueEndedForMaster);
            _dialogue.OnLineDisplayed += OnDialogueLine;
        }

        // ==================== Overlay ====================

        private void BuildOverlay()
        {
            _overlayGo = new GameObject("EndingOverlay");
            _overlayGo.SetActive(false);
            Object.DontDestroyOnLoad(_overlayGo);

            _bgSr = _overlayGo.AddComponent<SpriteRenderer>();
            _bgSr.sortingLayerName = PopupLayer;
            _bgSr.sortingOrder = 200;
            _bgSr.sprite = CreateBlackSprite();

            FitToCamera(_overlayGo, 9f);
        }

        private static void FitToCamera(GameObject go, float zOffset)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var hh = cam.orthographicSize;
            var hw = hh * cam.aspect;
            go.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z + zOffset);
            go.transform.localScale = new Vector3(hw * 2f, hh * 2f, 1f);
        }

        /// <summary>根据 sprite 尺寸自适应缩放，填满相机画面不变形</summary>
        private static void FitSpriteToCamera(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            var camH = cam.orthographicSize * 2f;
            var camW = camH * cam.aspect;
            var sprW = sr.sprite.bounds.size.x;
            var sprH = sr.sprite.bounds.size.y;
            var scale = Mathf.Max(camW / sprW, camH / sprH);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static Sprite CreateBlackSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.black);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        // ==================== Phase Handling ====================

        // ==================== Master Appearance (节点17后) ====================

        private void OnDialogueLine(int lineIndex, int total)
        {
            if (!_active || _endingType != GamePhase.Phase7_Epilogue_B) return;
            // line 0 已在 StartEnding 中显示，从 line 1 开始跟
            if (lineIndex > 0 && lineIndex < _bSprites.Count)
                ShowBImage(lineIndex);
        }

        private void OnDialogueEndedForMaster(DialogueEndedEvent e)
        {
            if (e.Def == null) return;
            if (e.Def.name == "Def_LivingRoomMirror")
                SpawnMaster();
            else if (e.Def.name == "Def_SofaOwner")
                ShowEndingChoice();
        }

        private async void SpawnMaster()
        {
            if (_masterSpawned) return;
            _masterSpawned = true;

            Debug.Log("[EndingDirector] Spawning master...");

            try
            {
                var assetHandle = Addressables.LoadAssetAsync<GameObject>(MasterKey);
                await assetHandle.Task;

                if (assetHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[EndingDirector] Failed to load master asset: {assetHandle.Status}");
                    _masterSpawned = false;
                    return;
                }

                var go = Object.Instantiate(assetHandle.Result);
                Addressables.Release(assetHandle);
                go.SetActive(false);
                go.transform.position = new Vector3(0.389999986f, -0.460000008f, 0f);

                var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in srs) sr.color = new Color(1f, 1f, 1f, 0f);

                go.SetActive(true);
                IsAnimating = true;

                for (int i = 0; i < srs.Length; i++)
                {
                    var t = srs[i].DOFade(1f, 2f).SetEase(Ease.InOutQuad);
                    if (i == srs.Length - 1)
                        t.OnComplete(() => { IsAnimating = false; Debug.Log("[EndingDirector] Master fade complete"); });
                }
                if (srs.Length == 0) IsAnimating = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EndingDirector] SpawnMaster failed: {ex.Message}");
                IsAnimating = false;
                _masterSpawned = false;
            }
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Phase7_Epilogue_A)
                StartEnding(GamePhase.Phase7_Epilogue_A);
            else if (phase == GamePhase.Phase7_Epilogue_B)
                StartEnding(GamePhase.Phase7_Epilogue_B);
            else if (phase == GamePhase.Phase6_InnerLivingRoom_Mirror)
            {
                _masterSpawned = false;
                // 从存档恢复时，如果镜子 beat 已完成，直接显示女主人
                if (_flow.GameData?.CompletedBeatIds?.Contains("Def_LivingRoomMirror") == true)
                    SpawnMaster();
            }
            else if (_active)
                Cleanup();
        }

        private async void StartEnding(GamePhase type)
        {
            _active = true;
            _endingType = type;
            _imageIndex = 0;
            _dialogueFinished = false;
            _showingAfter = false;
            _spritePreloaded = false;

            // 先显示黑底
            _bgSr.sprite = CreateBlackSprite();
            _overlayGo.SetActive(true);
            FitToCamera(_overlayGo, 9f);

            // 预加载图片
            await PreloadSprites(type);

            _spritePreloaded = true;

            // 显示第一张图（带淡入）
            if (type == GamePhase.Phase7_Epilogue_A)
            {
                _bgSr.sprite = await LoadSprite(EndingAKey);
                FitSpriteToCamera(_bgSr);
                _bgSr.color = new Color(1f, 1f, 1f, 0f);
                await FadeBgAlpha(0f, 1f, 1f);
            }
            else
            {
                if (_bSprites.Count > 0) ShowBImage(0);
            }
        }

        private async System.Threading.Tasks.Task PreloadSprites(GamePhase type)
        {
            if (type == GamePhase.Phase7_Epilogue_B)
            {
                _bSprites.Clear();
                for (int i = 0; i < EndingBKeys.Length; i++)
                {
                    if (i == 5)
                    {
                        _bSprites.Add(null); // 视频占位
                        // 预加载视频避免播放时卡顿
                        var vHandle = Addressables.LoadAssetAsync<VideoClip>(EndingBKeys[i]);
                        await vHandle.Task;
                        _videoClip = vHandle.Status == AsyncOperationStatus.Succeeded ? vHandle.Result : null;
                        continue;
                    }
                    var s = await LoadSprite(EndingBKeys[i]);
                    _bSprites.Add(s);
                }
            }
            _afterABSprite = await LoadSprite(AfterABKey);
            _characterSprite = await LoadSprite(CharacterKey);
        }

        // ==================== Tick ====================

        protected override void Tick(float dt)
        {
            if (_choiceActive) { CheckChoiceClick(); return; }
            if (!_active || !_spritePreloaded) return;

            // 检测对话是否播完
            if (!_dialogueFinished && _dialogue != null && !_dialogue.IsPlaying)
                _dialogueFinished = true;

            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            bool clicked;
            if (mouse != null) clicked = mouse.leftButton.wasPressedThisFrame;
            else if (touch != null) clicked = touch.primaryTouch.press.wasPressedThisFrame;
            else return;

            if (!clicked) return;

            if (_showingAfter)
            {
                ReturnToMainMenu();
                return;
            }

            // 对话播完 + 所有图展示完 + 动画结束
            if (_dialogueFinished && IsAllShown() && !_transitioning && !_showingAfter)
            {
                ShowAfter(); // 显示 AfterAB 结尾图
                return;
            }
        }

        private bool IsAllShown()
        {
            if (_endingType == GamePhase.Phase7_Epilogue_A)
                return true;
            return _lastShownLine >= _bSprites.Count - 1;
        }

        private int _lastShownLine = -1;

        // ==================== Image Display ====================

        private GameObject _end07Go;
        private bool _transitioning;

        private async void ShowBImage(int index)
        {
            if (index < 0 || index >= _bSprites.Count) return;
            if (_transitioning) return;
            _transitioning = true;
            IsAnimating = true;
            _lastShownLine = index;

            // 1. 淡入黑屏（当前图 alpha→1，然后切黑）
            _bgSr.color = new Color(1f, 1f, 1f, 1f);
            _bgSr.sprite = CreateBlackSprite();
            FitToCamera(_overlayGo, 9f);

            if (index != 5)
                StopVideo();

            // 2. 换图 + 设透明
            if (index == 6 && _bSprites[6] != null)
            {
                // 结尾：AfterAB 做背景 + end07 叠在上面
                _bgSr.sprite = _afterABSprite ?? CreateBlackSprite();
                FitSpriteToCamera(_bgSr);

                if (_end07Go == null)
                {
                    _end07Go = new GameObject("End07Overlay");
                    Object.DontDestroyOnLoad(_end07Go);
                    var sr07 = _end07Go.AddComponent<SpriteRenderer>();
                    sr07.sortingLayerName = PopupLayer;
                    sr07.sortingOrder = 202;
                }
                _end07Go.SetActive(false);
                _end07Go.GetComponent<SpriteRenderer>().sprite = _bSprites[6];
                var cam = Camera.main;
                if (cam != null)
                    _end07Go.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z + 8f);
            }
            else if (index != 5 && _bSprites[index] != null)
            {
                if (_end07Go != null) _end07Go.SetActive(false);
                _bgSr.sprite = _bSprites[index];
                FitSpriteToCamera(_bgSr);
            }
            _bgSr.color = new Color(1f, 1f, 1f, 0f);

            // 3. 淡出黑屏
            await FadeBgAlpha(0f, 1f, 0.6f);

            // 4. 视频在完全淡出后播放
            if (index == 5) PlayVideo();

            // 5. end07 在淡出后显示
            if (index == 6 && _end07Go != null)
                _end07Go.SetActive(true);

            _transitioning = false;
            IsAnimating = false;
            _cooldownUntil = Time.time + 0.3f;
        }

        private async System.Threading.Tasks.Task FadeBgAlpha(float from, float to, float duration)
        {
            if (_bgSr == null) return;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                _bgSr.color = new Color(1f, 1f, 1f, a);
                await System.Threading.Tasks.Task.Yield();
            }
            _bgSr.color = new Color(1f, 1f, 1f, to);
        }

        private void ShowAfter()
        {
            _showingAfter = true;
            StopVideo();
            if (_end07Go != null) _end07Go.SetActive(false);

            _bgSr.sprite = _afterABSprite ?? CreateBlackSprite();
            FitSpriteToCamera(_bgSr);
        }

        // ==================== Video ====================

        private async void PlayVideo()
        {
            if (_videoGo == null)
            {
                _videoGo = new GameObject("EndingVideo");
                _videoGo.transform.SetParent(_overlayGo.transform, false);
                _videoGo.transform.localPosition = Vector3.zero;
                _videoGo.transform.localScale = Vector3.one; // 填满 overlay（overlay 已填满相机）

                _videoPlayer = _videoGo.AddComponent<VideoPlayer>();
                _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                _videoPlayer.isLooping = false;
                _videoPlayer.playOnAwake = false;

                _videoRT = new RenderTexture(1920, 1080, 0);
                _videoPlayer.targetTexture = _videoRT;

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "VideoQuad";
                _videoQuad = quad;
                quad.transform.SetParent(_overlayGo.transform, false);
                quad.transform.localPosition = Vector3.zero;
                quad.transform.localRotation = Quaternion.identity;

                // overlay 已缩放到填满相机，quad localScale=(1,1,1) 即可
                quad.transform.localScale = Vector3.one;

                var qr = quad.GetComponent<MeshRenderer>();
                qr.material = new Material(Shader.Find("Unlit/Texture"));
                qr.material.mainTexture = _videoRT;
                qr.sortingLayerName = PopupLayer;
                qr.sortingOrder = 201;

            }

            _videoGo.SetActive(true);
            _videoPlayer.clip = _videoClip;
            _videoPlayer.Prepare();
            while (!_videoPlayer.isPrepared) await System.Threading.Tasks.Task.Yield();
            _videoPlayer.Play();
        }

        private void StopVideo()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                Object.Destroy(_videoPlayer);
                _videoPlayer = null;
            }
            if (_videoGo != null)
            {
                Object.Destroy(_videoGo);
                _videoGo = null;
            }
            if (_videoQuad != null)
            {
                Object.Destroy(_videoQuad);
                _videoQuad = null;
            }
        }

        // ==================== Choice UI ====================

        private void ShowEndingChoice()
        {
            if (_choiceActive) return;
            _choiceActive = true;
            IsAnimating = true;

            var cam = Camera.main;
            if (cam == null) return;

            var camPos = cam.transform.position;
            var hh = cam.orthographicSize;
            var hw = hh * cam.aspect;

            var labelGo = new GameObject("ChoiceLabel");
            labelGo.transform.position = new Vector3(camPos.x, camPos.y + 1.5f, camPos.z + 7f);
            var labelSr = labelGo.AddComponent<SpriteRenderer>();
            labelSr.sortingLayerName = PopupLayer;
            labelSr.sortingOrder = 195;
            labelSr.sprite = CreateTextSprite("你要怎么选择呢？", 48, Color.white);
            labelSr.transform.localScale = Vector3.one * 0.8f * hh / 5.4f;
            Object.DontDestroyOnLoad(labelGo);

            _stayBtn = CreateChoiceButton("留下", new Vector3(camPos.x - 1.2f, camPos.y - 0.2f, camPos.z + 7f));
            _leaveBtn = CreateChoiceButton("离开", new Vector3(camPos.x + 1.2f, camPos.y - 0.2f, camPos.z + 7f));

            // 点击时隐藏选择 UI
            _choiceLabelGo = labelGo;
        }

        private GameObject CreateChoiceButton(string text, Vector3 pos)
        {
            var go = new GameObject($"Choice_{text}");
            go.transform.position = pos;
            Object.DontDestroyOnLoad(go);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = PopupLayer;
            sr.sortingOrder = 196;
            sr.sprite = CreateButtonSprite(text);
            sr.color = new Color(1f, 1f, 1f, 0.85f);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.5f, 1.2f);
            go.layer = LayerMask.NameToLayer("UI");

            return go;
        }

        private void CheckChoiceClick()
        {
            if (!_choiceActive) return;

            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            bool clicked;
            Vector2 pointerPos;

            if (mouse != null)
            {
                pointerPos = mouse.position.ReadValue();
                clicked = mouse.leftButton.wasPressedThisFrame;
            }
            else if (touch != null)
            {
                pointerPos = touch.primaryTouch.position.ReadValue();
                clicked = touch.primaryTouch.press.wasPressedThisFrame;
            }
            else return;

            if (!clicked) return;

            var cam = Camera.main;
            if (cam == null) return;

            var worldPos = (Vector2)cam.ScreenToWorldPoint(pointerPos);
            var hits = new List<Collider2D>();
            Physics2D.OverlapPoint(worldPos, new ContactFilter2D().NoFilter(), hits);

            foreach (var hit in hits)
            {
                if (_stayBtn != null && hit.gameObject == _stayBtn)
                {
                    CleanupChoice();
                    _flow.TriggerEndingTransition(GamePhase.Phase7_Epilogue_A);
                    return;
                }
                if (_leaveBtn != null && hit.gameObject == _leaveBtn)
                {
                    CleanupChoice();
                    _flow.TriggerEndingTransition(GamePhase.Phase7_Epilogue_B);
                    return;
                }
            }
        }

        private void CleanupChoice()
        {
            _choiceActive = false;
            IsAnimating = false;
            if (_stayBtn != null) { Object.Destroy(_stayBtn); _stayBtn = null; }
            if (_leaveBtn != null) { Object.Destroy(_leaveBtn); _leaveBtn = null; }
            if (_choiceLabelGo != null) { Object.Destroy(_choiceLabelGo); _choiceLabelGo = null; }
        }

        private static Sprite CreateTextSprite(string text, int fontSize, Color color)
        {
            var tex = new Texture2D(512, 64);
            var pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 512, 64), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateButtonSprite(string text)
        {
            var tex = new Texture2D(256, 96);
            var pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0.2f, 0.15f, 0.3f, 0.9f);
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 256, 96), new Vector2(0.5f, 0.5f));
        }

        // ==================== Cleanup ====================

        private async void ReturnToMainMenu()
        {
            _flow.GetSaveState();
            Debug.Log("[EndingDirector] Returning to main menu...");
            try
            {
                var fadeGo = new GameObject("EndFade");
                var cg = fadeGo.AddComponent<CanvasGroup>();
                var canvas = fadeGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;
                var img = fadeGo.AddComponent<UnityEngine.UI.Image>();
                img.color = Color.black;
                cg.alpha = 0f;
                Object.DontDestroyOnLoad(fadeGo);
                var elapsed = 0f;
                while (elapsed < 0.8f) { elapsed += Time.deltaTime; cg.alpha = Mathf.Clamp01(elapsed / 0.8f); await System.Threading.Tasks.Task.Yield(); }
                cg.alpha = 1f;
                Cleanup();
                Object.Destroy(fadeGo); // 销毁黑屏遮罩
                var handle = Addressables.LoadSceneAsync("Scenes/Start New", LoadSceneMode.Single);
                await handle.Task;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EndingDirector] ReturnToMainMenu failed: {ex.Message}");
                Cleanup();
                SceneManager.LoadSceneAsync("Start New");
            }
        }

        private void Cleanup()
        {
            _active = false;
            StopVideo();
            if (_end07Go != null) _end07Go.SetActive(false);
            _overlayGo.SetActive(false);
            _bSprites.Clear();
            _spritePreloaded = false;
        }

        // ==================== Util ====================

        private static async System.Threading.Tasks.Task<Sprite> LoadSprite(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            try
            {
                var handle = Addressables.LoadAssetAsync<Sprite>(key);
                await handle.Task;
                return handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EndingDirector] Failed to load '{key}': {ex.Message}");
                return null;
            }
        }

        protected override void OnShutdown()
        {
            _flow.OnPhaseChanged -= OnPhaseChanged;
            _events?.Unsubscribe<DialogueEndedEvent>(OnDialogueEndedForMaster);
            _dialogue.OnLineDisplayed -= OnDialogueLine;
            Cleanup();
            if (_overlayGo != null) Destroy(_overlayGo);
            if (_videoRT != null) Destroy(_videoRT);
        }
    }
}
