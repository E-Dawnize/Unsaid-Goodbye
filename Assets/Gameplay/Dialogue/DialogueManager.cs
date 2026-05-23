using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Architecture.Interfaces;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
using Gameplay.Audio;
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

        private static Font _dialogueFont;

        [Inject] private IEventCenter _events;
        [Inject] private IPlayerInput _input;
        [InjectOptional] private IAudioManager _audio;

        private const string DialogueAdvanceSfxKey = "SFX/DialogueAdvance";

        private CanvasGroup _dialogueGroup;
        private Image _panelImage;
        private Text _speakerText;
        private Text _dialogueText;
        private RectTransform _choiceRoot;
        private readonly List<GameObject> _choiceObjects = new();
        private readonly Queue<DialogueSequence> _pendingSequences = new();
        private int _selectedChoiceIndex = -1;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

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

            foreach (var entry in data.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Text) && entry.Choices.Count == 0)
                    continue;

                ApplyEntryStyle(entry);
                _speakerText.text = BuildSpeakerText(entry);
                _dialogueText.text = entry.Text ?? string.Empty;
                _audio?.PlaySfx(DialogueAdvanceSfxKey);
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
            _choiceRoot.anchorMax = new Vector2(1f, 0.26f);
            _choiceRoot.offsetMin = new Vector2(72f, 14f);
            _choiceRoot.offsetMax = new Vector2(-72f, 0f);

            var layout = choicesObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
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

        private async Task WaitForChoice(DialogueEntryData entry)
        {
            _selectedChoiceIndex = -1;

            for (var i = 0; i < entry.Choices.Count; i++)
                CreateChoiceButton(i, entry.Choices[i].Text);

            while (_selectedChoiceIndex < 0)
                await Task.Yield();

            var selected = entry.Choices[_selectedChoiceIndex];
            ClearChoices();

            if (!string.IsNullOrWhiteSpace(selected.ResultDialogueId))
                await PlayAndWait(selected.ResultDialogueId);
        }

        private void CreateChoiceButton(int index, string label)
        {
            var buttonObject = new GameObject($"Choice_{index}");
            buttonObject.transform.SetParent(_choiceRoot, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);

            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => _selectedChoiceIndex = index);

            var text = CreateText("Text", buttonObject.transform, 24, FontStyle.Normal);
            text.text = $"「{label}」";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.27f, 0.16f, 0.09f, 1f);

            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 4f);
            rect.offsetMax = new Vector2(-12f, -4f);

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
            // 跳过当前帧的残余点击，等下一帧再开始检测
            await Task.Yield();
            await Task.Yield();

            if (_input == null) return;

            while (!_input.IsClickTriggered)
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
            public Color TextColor;
            public bool AutoAdvance;
            public float AutoAdvanceDelay;
            public readonly List<DialogueChoiceData> Choices = new();

            public static DialogueEntryData From(DialogueEntry entry)
            {
                var data = new DialogueEntryData
                {
                    Type = entry.Type,
                    Speaker = entry.Speaker,
                    Text = entry.Text,
                    TextColor = entry.TextColor,
                    AutoAdvance = entry.AutoAdvance,
                    AutoAdvanceDelay = entry.AutoAdvanceDelay
                };

                if (entry.Choices != null)
                {
                    foreach (var choice in entry.Choices)
                    {
                        data.Choices.Add(new DialogueChoiceData
                        {
                            Text = choice.Text,
                            ResultDialogueId = choice.ResultDialogueId
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
        }
    }
}
