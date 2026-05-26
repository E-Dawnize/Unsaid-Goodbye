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
        [Inject] private Audio.IAudioManager _audio;

        private const string PopupLayer = "Popup";
        private const string MasterKey = "Ending/Master";
        private const string EndingAKey = "Arts/Ending/Ending-A";
        private const string AfterABKey = "Arts/Ending/AfterAB";
        private const string CharacterKey = "Arts/Ending/character-moon";

        /// <summary>动画进行中禁止移动/对话/切图</summary>
        public static bool IsAnimating { get; private set; }
        /// <summary>B 结局是否可选（L1-L5 + H0-H4 全收集）</summary>
        public static bool IsEndingBAvailable { get; private set; }
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
        private bool _dialogueFinished;
        private bool _dialogueStarted;
        private bool _showingAfter;
        private readonly List<Sprite> _bSprites = new();
        private Sprite _afterABSprite;
        private Sprite _characterSprite;
        private bool _spritePreloaded;

        // Choice UI
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
            _dialogueFinished = false;

            // 隐藏摇杆 + 静音 SFX
            var js = Object.FindObjectOfType<Input.UI.WorldSpaceJoystick>(true);
            if (js != null) js.gameObject.SetActive(false);
            _audio.SfxVolume = 0f;
            _dialogueStarted = false;
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
                    if (s == null) Debug.LogWarning($"[EndingDirector] Preload: {EndingBKeys[i]} failed");
                    _bSprites.Add(s);
                }
            }
            _afterABSprite = await LoadSprite(AfterABKey);
            _characterSprite = await LoadSprite(CharacterKey);
        }

        // ==================== Tick ====================

        protected override void Tick(float dt)
        {
            if (_choiceActive) return;
            if (!_active || !_spritePreloaded) return;

            // 跟踪对话是否开始过
            if (!_dialogueStarted && _dialogue != null && _dialogue.IsPlaying)
                _dialogueStarted = true;

            // 对话播完检测（必须对话先开始过，避免启动时的竞态）
            if (_dialogueStarted && !_dialogueFinished && _dialogue != null && !_dialogue.IsPlaying)
            {
                _dialogueFinished = true;
                // Ending A：对话结束自动切 AfterAB
                if (_endingType == GamePhase.Phase7_Epilogue_A && !_showingAfter)
                {
                    ShowAfter();
                    return;
                }
            }

            // Ending A 对话期间点击不处理（只推进对话，不切图）
            if (_endingType == GamePhase.Phase7_Epilogue_A && !_showingAfter)
                return;

            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            bool clicked;
            if (mouse != null) clicked = mouse.leftButton.wasPressedThisFrame;
            else if (touch != null) clicked = touch.primaryTouch.press.wasPressedThisFrame;
            else return;

            if (!clicked) return;

            // AfterAB 点击 → 回主页（A/B 共用）
            if (_showingAfter)
            {
                ReturnToMainMenu();
                return;
            }

            // Ending B：对话播完 + 所有图展示完 + 动画结束 → 点一下切 AfterAB
            if (_endingType == GamePhase.Phase7_Epilogue_B &&
                _dialogueFinished && IsAllShown() && !_transitioning && !_showingAfter)
            {
                ShowAfter();
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

            // 视频索引特殊处理：直接切黑底 + 播放，不走淡入淡出
            if (index == 5)
            {
                _bgSr.sprite = CreateBlackSprite();
                FitToCamera(_overlayGo, 9f);
                _bgSr.color = new Color(1f, 1f, 1f, 0f);
                PlayVideo();
                _transitioning = false;
                IsAnimating = false;
                return;
            }

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
            _audio?.PlayBgm("BGM/MainEnding");

            _bgSr.sprite = _afterABSprite ?? CreateBlackSprite();
            FitSpriteToCamera(_bgSr);
        }

        // ==================== Video ====================

        private async void PlayVideo()
        {
            if (_videoGo == null)
            {
                _videoGo = new GameObject("EndingVideo");
                _videoGo.SetActive(false);
                _videoGo.transform.SetParent(_overlayGo.transform, false);
                _videoGo.transform.localPosition = Vector3.zero;
                _videoGo.transform.localScale = Vector3.one;

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
                quad.transform.localScale = Vector3.one;

                var qr = quad.GetComponent<MeshRenderer>();
                var shader = Shader.Find("Unlit/Texture");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                qr.material = new Material(shader);
                qr.material.mainTexture = _videoRT;
                qr.sortingLayerName = PopupLayer;
                qr.sortingOrder = 201;

                // 加载视频
                if (_videoClip == null)
                {
                    var vHandle = Addressables.LoadAssetAsync<VideoClip>(EndingBKeys[5]);
                    await vHandle.Task;
                    _videoClip = vHandle.Status == AsyncOperationStatus.Succeeded ? vHandle.Result : null;
                }
            }

            if (_videoClip == null) return;
            _videoPlayer.clip = _videoClip;
            _videoGo.SetActive(true);
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

        private async void ShowEndingChoice()
        {
            if (_choiceActive) return;
            _choiceActive = true;
            IsAnimating = true;

            // B 结局条件
            IsEndingBAvailable = (_flow.GameData?.CollectedItemIds?.Count ?? 0) >= 10;

            // 对话系统处理选择
            await (_dialogue?.PlayAndWait("dialogue_ending_choice") ?? System.Threading.Tasks.Task.CompletedTask);

            var result = Dialogue.DialogueManager.LastChoiceResultId;
            var ending = (result == "LEAVE" && IsEndingBAvailable)
                ? GamePhase.Phase7_Epilogue_B : GamePhase.Phase7_Epilogue_A;
            _flow.TriggerEndingTransition(ending);

            _choiceActive = false;
            IsAnimating = false;
        }

        // ==================== Cleanup ====================

        private async void ReturnToMainMenu()
        {
            _flow.GetSaveState();
            _audio?.StopBgm(0.5f);
            // 恢复 SFX 音量
            _audio.SfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1f);
            // 恢复摇杆
            var js = Object.FindObjectOfType<Input.UI.WorldSpaceJoystick>(true);
            if (js != null) js.gameObject.SetActive(true);
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
                Object.Destroy(fadeGo);
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
            _audio?.StopBgm(0f);
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
