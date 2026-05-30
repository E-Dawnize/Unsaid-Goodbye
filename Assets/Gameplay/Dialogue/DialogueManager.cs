using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Architecture.Interfaces;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
using Gameplay.Audio;
using Gameplay.Ending;
using Gameplay.SO;
using Input.InputInterface;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Gameplay.Dialogue
{
    public class DialogueManager : IDialogueManager, IInitializable, System.IDisposable
    {
        private const string DialogueLabel = "DialogueSequence";
        private const string DialogueBoxResourcePath = "UI/Dialogue/DialogBox";
        private const string DialogueFontResourcePath = "Fonts/HongLeiXiaozhitiao";
        private const string BlackCatPortraitAddress = "Setting/Sound[Catlogo ]";
        private static readonly Vector2 BlackCatPortraitSize = new(200f, 200f);
        private static readonly Vector2 BlackCatPortraitPosition = new(200f, 80f);

        private static Font _dialogueFont;

        [Inject] private IEventCenter _events;
        [Inject] private IPlayerInput _input;
        [InjectOptional] private IAudioManager _audio;

        private const string DialogueAdvanceSfxKey = "SFX/DialogueAdvance";

        private CanvasGroup _dialogueGroup;
        private Image _panelImage;
        private RectTransform _dialoguePanel;
        private Image _speakerPortrait;
        private Text _speakerText;
        private Text _dialogueText;
        private RectTransform _choiceRoot;
        private readonly List<GameObject> _choiceObjects = new();
        private readonly Queue<DialogueSequence> _pendingSequences = new();
        private AsyncOperationHandle<Sprite> _blackCatPortraitHandle;
        private Sprite _blackCatPortraitSprite;
        private bool _blackCatPortraitLoadAttempted;
        private int _selectedChoiceIndex = -1;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;
        public event System.Action<int, int> OnLineDisplayed;
        /// <summary>最后一次选择结果（ResultDialogueId），供外部读取做分支</summary>
        public static string LastChoiceResultId { get; private set; }

        private int _currentLineIndex;
        private int _totalLines;

        public void Initialize()
        {
            if (_events == null)
            {
                Debug.LogError("[Dialogue] IEventCenter was not injected; interaction dialogue cannot subscribe.");
                return;
            }

            _events.Subscribe<InteractionEvent>(OnInteraction);
            Debug.Log("[Dialogue] Subscribed to InteractionEvent");
        }

        public void Dispose()
        {
            _events?.Unsubscribe<InteractionEvent>(OnInteraction);

            if (_blackCatPortraitHandle.IsValid())
                Addressables.Release(_blackCatPortraitHandle);
        }

        private void OnInteraction(InteractionEvent e)
        {
            if (e.Def == null) return;
            if (e.Def.Dialogue == null) return;
            if (_isPlaying)
            {
                _pendingSequences.Enqueue(e.Def.Dialogue);
                return;
            }

            _ = PlayAndWait(e.Def.Dialogue);
        }

        public async Task PlayAndWait(string dialogueId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId) || dialogueId == "0")
                return;

            var data = await LoadDialogueData(dialogueId);
            if (data == null)
                return;

            await PlayAndWait(data);
            await PlayPendingSequences();
        }

        public async Task PlayAndWait(DialogueSequence sequence)
        {
            if (sequence == null) return;
            await PlayAndWait(DialoguePlaybackData.From(sequence));
            await PlayPendingSequences();
        }

        private async Task PlayAndWait(DialoguePlaybackData data)
        {
            EnsureDialogueView();

            _dialogueGroup.alpha = 1f;
            _dialogueGroup.blocksRaycasts = true;
            _isPlaying = true;
            _currentLineIndex = 0;
            _totalLines = data.Entries.Count;

            foreach (var entry in data.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Text) && entry.Choices.Count == 0)
                    continue;

                // 隐藏/显示对话框背景
                _panelImage.enabled = !entry.HidePanel;

                ApplyEntryStyle(entry);
                if (entry.HidePanel)
                    ClearSpeakerVisual();
                else
                    await ApplySpeakerVisual(entry);
                _dialogueText.text = entry.Text ?? string.Empty;
                _audio?.PlaySfx(DialogueAdvanceSfxKey);
                await PlayEntrySfx(entry.SfxKey);
                OnLineDisplayed?.Invoke(_currentLineIndex, _totalLines);
                _currentLineIndex++;
                ClearChoices();

                if (entry.Choices.Count > 0)
                {
                    await WaitForChoice(entry);
                }
                else if (entry.AutoAdvance)
                {
                    await WaitSeconds(Mathf.Max(0f, entry.AutoAdvanceDelay));
                }
                else
                {
                    await WaitForAdvance();
                }
            }

            ClearChoices();
            _panelImage.enabled = true;
            _speakerText.text = string.Empty;
            _dialogueText.text = string.Empty;
            _dialogueGroup.alpha = 0f;
            _dialogueGroup.blocksRaycasts = false;
            _isPlaying = false;

            if (data.CompletionId != null)
                _events?.Publish(new DialogueEndedEvent { Def = data.CompletionId });
        }

        private async Task PlayPendingSequences()
        {
            if (_isPlaying) return;

            while (_pendingSequences.Count > 0)
            {
                var sequence = _pendingSequences.Dequeue();
                if (sequence == null) continue;
                await PlayAndWait(DialoguePlaybackData.From(sequence));
            }
        }

        private static async Task<DialoguePlaybackData> LoadDialogueData(string dialogueId)
        {
            var handle = Addressables.LoadAssetsAsync<DialogueSequence>(DialogueLabel, null, false);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[Dialogue] Failed to load dialogue label: {DialogueLabel}");
                return null;
            }

            foreach (var sequence in handle.Result)
            {
                if (sequence != null && sequence.DialogueId == dialogueId)
                {
                    var data = DialoguePlaybackData.From(sequence);
                    Addressables.Release(handle);
                    return data;
                }
            }

            Addressables.Release(handle);
            Debug.LogWarning($"[Dialogue] Dialogue not found: {dialogueId}");
            return null;
        }

        private void EnsureDialogueView()
        {
            if (_dialogueGroup != null) return;

            var root = new GameObject("DialogueCanvas");
            Object.DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue - 1;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            root.AddComponent<GraphicRaycaster>();

            _dialogueGroup = root.AddComponent<CanvasGroup>();
            _dialogueGroup.alpha = 0f;
            _dialogueGroup.blocksRaycasts = false;
            _dialogueGroup.interactable = true;

            var panelObject = new GameObject("DialoguePanel");
            panelObject.transform.SetParent(root.transform, false);

            var panelRect = panelObject.AddComponent<RectTransform>();
            _dialoguePanel = panelRect;
            panelRect.anchorMin = new Vector2(0.07f, 0.03f);
            panelRect.anchorMax = new Vector2(0.93f, 0.30f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var artObject = new GameObject("DialogueArt");
            artObject.transform.SetParent(panelObject.transform, false);
            _panelImage = artObject.AddComponent<Image>();
            var panelSprite = Resources.Load<Sprite>(DialogueBoxResourcePath);
            if (panelSprite != null)
            {
                _panelImage.sprite = panelSprite;
                _panelImage.type = Image.Type.Simple;
                _panelImage.color = Color.white;
            }
            else
            {
                _panelImage.color = new Color(0.08f, 0.07f, 0.1f, 0.88f);
                Debug.LogWarning($"[Dialogue] Dialogue box sprite not found at Resources/{DialogueBoxResourcePath}");
            }
            var artRect = _panelImage.GetComponent<RectTransform>();
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;

            var portraitObject = new GameObject("SpeakerPortrait");
            portraitObject.transform.SetParent(panelObject.transform, false);
            _speakerPortrait = portraitObject.AddComponent<Image>();
            _speakerPortrait.preserveAspect = true;
            _speakerPortrait.enabled = false;
            var portraitRect = _speakerPortrait.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 0.58f);
            portraitRect.anchorMax = new Vector2(0f, 0.88f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = BlackCatPortraitPosition;
            portraitRect.sizeDelta = BlackCatPortraitSize;

            _speakerText = CreateText("SpeakerText", panelObject.transform, 26, FontStyle.Bold);
            var speakerRect = _speakerText.GetComponent<RectTransform>();
            speakerRect.anchorMin = new Vector2(0f, 0.58f);
            speakerRect.anchorMax = new Vector2(1f, 0.88f);
            speakerRect.offsetMin = new Vector2(72f, 0f);
            speakerRect.offsetMax = new Vector2(-72f, 0f);

            _dialogueText = CreateText("DialogueText", panelObject.transform, 35, FontStyle.Normal);
            _dialogueText.alignment = TextAnchor.UpperLeft;
            var textRect = _dialogueText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.22f);
            textRect.anchorMax = new Vector2(1f, 0.64f);
            textRect.offsetMin = new Vector2(157f, 0f);
            textRect.offsetMax = new Vector2(-72f, 0f);

            var choicesObject = new GameObject("Choices");
            choicesObject.transform.SetParent(panelObject.transform, false);
            _choiceRoot = choicesObject.AddComponent<RectTransform>();
            _choiceRoot.anchorMin = new Vector2(0f, 0f);
            _choiceRoot.anchorMax = new Vector2(1f, 0.35f);
            _choiceRoot.offsetMin = new Vector2(160f, 20f);
            _choiceRoot.offsetMax = new Vector2(-160f, -10f);

            var layout = choicesObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static Text CreateText(string name, Transform parent, int size, FontStyle style)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.font = GetDialogueFont();
            text.fontSize = size;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Font GetDialogueFont()
        {
            if (_dialogueFont != null)
                return _dialogueFont;

            _dialogueFont = Resources.Load<Font>(DialogueFontResourcePath);
            if (_dialogueFont == null)
            {
                Debug.LogWarning($"[Dialogue] Dialogue font not found at Resources/{DialogueFontResourcePath}");
                _dialogueFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return _dialogueFont;
        }

        private void ApplyEntryStyle(DialogueEntryData entry)
        {
            if (_panelImage.sprite != null)
                _panelImage.color = Color.white;

            switch (entry.Type)
            {
                case DialogueLineType.InnerMonologue:
                    _speakerText.color = new Color(0.31f, 0.18f, 0.24f, 1f);
                    _dialogueText.color = ResolveTextColor(entry, new Color(0.30f, 0.19f, 0.12f, 1f));
                    break;
                case DialogueLineType.Interaction:
                    _speakerText.color = new Color(0.34f, 0.19f, 0.11f, 1f);
                    _dialogueText.color = ResolveTextColor(entry, new Color(0.27f, 0.16f, 0.09f, 1f));
                    break;
                case DialogueLineType.StoryText:
                    _speakerText.color = new Color(0.32f, 0.16f, 0.12f, 1f);
                    _dialogueText.color = ResolveTextColor(entry, new Color(0.42f, 0.16f, 0.11f, 1f));
                    break;
                default:
                    _speakerText.color = new Color(0.34f, 0.19f, 0.11f, 1f);
                    _dialogueText.color = ResolveTextColor(entry, new Color(0.27f, 0.16f, 0.09f, 1f));
                    break;
            }
        }

        private static Color ResolveTextColor(DialogueEntryData entry, Color fallback)
            => entry.TextColor.a > 0f ? entry.TextColor : fallback;

        private async Task PlayEntrySfx(string sfxKey)
        {
            if (string.IsNullOrWhiteSpace(sfxKey))
                return;

            var configuredVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("SfxVolume", 0.5f));
            await PlayEntrySfxDirect(sfxKey, configuredVolume);
        }

        private static async Task PlayEntrySfxDirect(string sfxKey, float volume)
        {
            if (volume <= 0f)
                return;

            var handle = Addressables.LoadAssetAsync<AudioClip>(sfxKey);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogWarning($"[Dialogue] Entry SFX not found: {sfxKey}");
                if (handle.IsValid())
                    Addressables.Release(handle);
                return;
            }

            var clip = handle.Result;
            Debug.Log($"[Dialogue] Playing entry SFX: {sfxKey} vol={volume}");

            var go = new GameObject("DialogueEntrySfx");
            Object.DontDestroyOnLoad(go);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.clip = clip;
            source.Play();

            _ = CleanupEntrySfx(go, handle, clip.length);
        }

        private static async Task CleanupEntrySfx(GameObject go, AsyncOperationHandle<AudioClip> handle, float delay)
        {
            var elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.deltaTime;
                await Task.Yield();
            }

            if (go != null)
                Object.Destroy(go);

            if (handle.IsValid())
                Addressables.Release(handle);
        }

        private static string BuildSpeakerText(DialogueEntryData entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.Speaker))
                return entry.Speaker;

            return entry.Type switch
            {
                DialogueLineType.InnerMonologue => "黑猫",
                DialogueLineType.Interaction => "互动",
                DialogueLineType.StoryText => "剧情",
                _ => string.Empty
            };
        }

        private async Task ApplySpeakerVisual(DialogueEntryData entry)
        {
            var showBlackCatPortrait = IsBlackCatLine(entry);
            if (showBlackCatPortrait)
            {
                var portrait = await GetBlackCatPortrait();
                if (portrait != null)
                {
                    _speakerText.text = string.Empty;
                    _speakerPortrait.sprite = portrait;
                    _speakerPortrait.enabled = true;
                    return;
                }
            }

            if (_speakerPortrait != null)
            {
                _speakerPortrait.enabled = false;
                _speakerPortrait.sprite = null;
            }

            _speakerText.text = showBlackCatPortrait ? string.Empty : BuildSpeakerText(entry);
        }

        private void ClearSpeakerVisual()
        {
            if (_speakerPortrait != null)
            {
                _speakerPortrait.enabled = false;
                _speakerPortrait.sprite = null;
            }

            _speakerText.text = string.Empty;
        }

        private static bool IsBlackCatLine(DialogueEntryData entry)
        {
            if (entry.Type == DialogueLineType.InnerMonologue)
                return true;

            if (!string.IsNullOrWhiteSpace(entry.Speaker) && entry.Speaker.Trim() == "\u9ed1\u732b")
                return true;

            return !string.IsNullOrWhiteSpace(entry.Speaker) && entry.Speaker.Trim() == "黑猫";
        }

        private async Task<Sprite> GetBlackCatPortrait()
        {
            if (_blackCatPortraitSprite != null)
                return _blackCatPortraitSprite;

            if (_blackCatPortraitLoadAttempted)
                return null;

            _blackCatPortraitLoadAttempted = true;
            _blackCatPortraitHandle = Addressables.LoadAssetAsync<Sprite>(BlackCatPortraitAddress);
            await _blackCatPortraitHandle.Task;

            if (_blackCatPortraitHandle.Status != AsyncOperationStatus.Succeeded || _blackCatPortraitHandle.Result == null)
            {
                Debug.LogWarning($"[Dialogue] Black cat portrait not found: {BlackCatPortraitAddress}");
                return null;
            }

            _blackCatPortraitSprite = _blackCatPortraitHandle.Result;
            return _blackCatPortraitSprite;
        }

        private async Task WaitForChoice(DialogueEntryData entry)
        {
            _selectedChoiceIndex = -1;

            for (var i = 0; i < entry.Choices.Count; i++)
                CreateChoiceButton(i, entry.Choices[i].Text, entry.Choices[i].SpriteKey);

            while (_selectedChoiceIndex < 0)
                await Task.Yield();

            var selected = entry.Choices[_selectedChoiceIndex];
            ClearChoices();
            LastChoiceResultId = selected.ResultDialogueId ?? "";

            if (!string.IsNullOrWhiteSpace(selected.ResultDialogueId))
                await PlayAndWait(selected.ResultDialogueId);
        }

        private async void CreateChoiceButton(int index, string label, string spriteKey)
        {
            var buttonObject = new GameObject($"Choice_{index}", typeof(RectTransform));
            buttonObject.transform.SetParent(_choiceRoot, false);

            var button = buttonObject.AddComponent<Button>();
            var capturedIndex = index;
            button.onClick.AddListener(() => _selectedChoiceIndex = capturedIndex);

            if (!string.IsNullOrEmpty(spriteKey))
            {
                // 挂到对话面板上，用锚点定位
                buttonObject.transform.SetParent(_dialoguePanel, false);
                var rt = buttonObject.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(index == 0 ? 0.20f : 0.58f, 0f);
                rt.anchorMax = new Vector2(index == 0 ? 0.42f : 0.80f, 0.60f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var image = buttonObject.AddComponent<Image>();
                var canInteract = !(label == "离开" && !EndingDirector.IsEndingBAvailable);
                image.color = canInteract ? Color.white : Color.gray;
                button.interactable = canInteract;
                var handle = Addressables.LoadAssetAsync<Sprite>(spriteKey);
                await handle.Task;
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                    image.sprite = handle.Result;
                else
                    Debug.LogWarning($"[Dialogue] Failed to load choice sprite: {spriteKey}, status={handle.Status}");
            }
            else
            {
                var image = buttonObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.92f);

                var text = CreateText("Text", buttonObject.transform, 24, FontStyle.Normal);
                text.text = $"「{label}」";
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(0.27f, 0.16f, 0.09f, 1f);

                var rect = text.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(12f, 4f);
                rect.offsetMax = new Vector2(-12f, -4f);
            }

            _choiceObjects.Add(buttonObject);
        }

        private void ClearChoices()
        {
            foreach (var choiceObject in _choiceObjects)
            {
                if (choiceObject != null)
                    Object.Destroy(choiceObject);
            }

            _choiceObjects.Clear();
            _selectedChoiceIndex = -1;
        }

        private async Task WaitForAdvance()
        {
            await Task.Yield();
            await Task.Yield();

            if (_input == null) return;

            while (!_input.IsClickTriggered || EndingDirector.IsAnimating)
                await Task.Yield();
        }

        private static async Task WaitSeconds(float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                await Task.Yield();
            }
        }

        private class DialoguePlaybackData
        {
            public InteractionDef CompletionId;
            public readonly List<DialogueEntryData> Entries = new();

            public static DialoguePlaybackData From(DialogueSequence sequence)
            {
                var data = new DialoguePlaybackData
                {
                    CompletionId = sequence.CompletionId
                };

                if (sequence.Entries != null && sequence.Entries.Count > 0)
                {
                    foreach (var entry in sequence.Entries)
                        data.Entries.Add(DialogueEntryData.From(entry));
                }
                else if (sequence.Lines != null)
                {
                    foreach (var line in sequence.Lines)
                    {
                        data.Entries.Add(new DialogueEntryData
                        {
                            Type = DialogueLineType.Narration,
                            Text = line
                        });
                    }
                }

                return data;
            }
        }

        private class DialogueEntryData
        {
            public DialogueLineType Type;
            public string Speaker;
            public string Text;
            public string SfxKey;
            public Color TextColor;
            public bool AutoAdvance;
            public float AutoAdvanceDelay;
            public bool HidePanel;
            public readonly List<DialogueChoiceData> Choices = new();

            public static DialogueEntryData From(DialogueEntry entry)
            {
                var data = new DialogueEntryData
                {
                    Type = entry.Type,
                    Speaker = entry.Speaker,
                    Text = entry.Text,
                    SfxKey = entry.SfxKey,
                    TextColor = entry.TextColor,
                    AutoAdvance = entry.AutoAdvance,
                    AutoAdvanceDelay = entry.AutoAdvanceDelay,
                    HidePanel = entry.HidePanel
                };

                if (entry.Choices != null)
                {
                    foreach (var choice in entry.Choices)
                    {
                        data.Choices.Add(new DialogueChoiceData
                        {
                            Text = choice.Text,
                            ResultDialogueId = choice.ResultDialogueId,
                            SpriteKey = choice.SpriteKey
                        });
                    }
                }

                return data;
            }
        }

        private class DialogueChoiceData
        {
            public string Text;
            public string ResultDialogueId;
            public string SpriteKey;
        }
    }
}
