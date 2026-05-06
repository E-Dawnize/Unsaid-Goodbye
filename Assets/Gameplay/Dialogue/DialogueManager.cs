using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Architecture.Interfaces;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
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

        [Inject] private IEventCenter _events;
        [Inject] private IPlayerInput _input;

        private CanvasGroup _dialogueGroup;
        private Image _panelImage;
        private Text _speakerText;
        private Text _dialogueText;
        private RectTransform _choiceRoot;
        private readonly List<GameObject> _choiceObjects = new();
        private int _selectedChoiceIndex = -1;
        private bool _isPlaying;

        public void Initialize()
        {
            _events.Subscribe<InteractionEvent>(OnInteraction);
        }

        public void Dispose()
        {
            _events?.Unsubscribe<InteractionEvent>(OnInteraction);
        }

        private void OnInteraction(InteractionEvent e)
        {
            if (e.Def == null || e.Def.Dialogue == null || _isPlaying) return;
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
        }

        public async Task PlayAndWait(DialogueSequence sequence)
        {
            if (sequence == null) return;
            await PlayAndWait(DialoguePlaybackData.From(sequence));
        }

        private async Task PlayAndWait(DialoguePlaybackData data)
        {
            EnsureDialogueView();

            _input?.Disable();

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

            _input?.Enable();
            _isPlaying = false;

            if (data.CompletionId != null)
                _events?.Publish(new DialogueEndedEvent { Def = data.CompletionId });
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

            _panelImage = panelObject.AddComponent<Image>();

            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.06f);
            panelRect.anchorMax = new Vector2(0.92f, 0.3f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            _speakerText = CreateText("SpeakerText", panelObject.transform, 26, FontStyle.Bold);
            var speakerRect = _speakerText.GetComponent<RectTransform>();
            speakerRect.anchorMin = new Vector2(0f, 0.72f);
            speakerRect.anchorMax = new Vector2(1f, 1f);
            speakerRect.offsetMin = new Vector2(32f, 0f);
            speakerRect.offsetMax = new Vector2(-32f, -12f);

            _dialogueText = CreateText("DialogueText", panelObject.transform, 30, FontStyle.Normal);
            _dialogueText.alignment = TextAnchor.UpperLeft;
            var textRect = _dialogueText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.12f);
            textRect.anchorMax = new Vector2(1f, 0.75f);
            textRect.offsetMin = new Vector2(32f, 0f);
            textRect.offsetMax = new Vector2(-32f, 0f);

            var choicesObject = new GameObject("Choices");
            choicesObject.transform.SetParent(panelObject.transform, false);
            _choiceRoot = choicesObject.AddComponent<RectTransform>();
            _choiceRoot.anchorMin = new Vector2(0f, 0f);
            _choiceRoot.anchorMax = new Vector2(1f, 0.22f);
            _choiceRoot.offsetMin = new Vector2(32f, 16f);
            _choiceRoot.offsetMax = new Vector2(-32f, 0f);

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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private void ApplyEntryStyle(DialogueEntryData entry)
        {
            switch (entry.Type)
            {
                case DialogueLineType.InnerMonologue:
                    _panelImage.color = new Color(0.08f, 0.07f, 0.1f, 0.88f);
                    _speakerText.color = new Color(0.78f, 0.7f, 1f, 1f);
                    _dialogueText.color = ResolveTextColor(entry, new Color(0.9f, 0.84f, 1f, 1f));
                    break;
                case DialogueLineType.Interaction:
                    _panelImage.color = new Color(0.94f, 0.97f, 1f, 0.9f);
                    _speakerText.color = new Color(0.1f, 0.27f, 0.48f, 1f);
                    _dialogueText.color = ResolveTextColor(entry, new Color(0.05f, 0.12f, 0.2f, 1f));
                    break;
                case DialogueLineType.StoryText:
                    _panelImage.color = new Color(0.08f, 0.06f, 0.08f, 0.9f);
                    _speakerText.color = new Color(0.85f, 0.72f, 1f, 1f);
                    _dialogueText.color = ResolveTextColor(entry, new Color(0.86f, 0.22f, 0.24f, 1f));
                    break;
                default:
                    _panelImage.color = new Color(1f, 1f, 1f, 0.9f);
                    _speakerText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
                    _dialogueText.color = ResolveTextColor(entry, new Color(0.08f, 0.08f, 0.08f, 1f));
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
            text.color = new Color(0.06f, 0.1f, 0.18f, 1f);

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
            await Task.Yield();

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
