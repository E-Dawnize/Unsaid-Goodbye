using System.Threading.Tasks;
using Gameplay.SO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Gameplay.Dialogue
{
    public class DialogueManager : IDialogueManager
    {
        private const string DialogueLabel = "DialogueSequence";

        private CanvasGroup _dialogueGroup;
        private Text _dialogueText;

        public async Task PlayAndWait(string dialogueId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId))
                return;

            var lines = await LoadDialogueLines(dialogueId);
            if (lines == null)
                return;

            EnsureDialogueView();

            _dialogueGroup.alpha = 1f;
            _dialogueGroup.blocksRaycasts = true;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                _dialogueText.text = line;
                await WaitForAdvance();
            }

            _dialogueText.text = string.Empty;
            _dialogueGroup.alpha = 0f;
            _dialogueGroup.blocksRaycasts = false;
        }

        private static async Task<string[]> LoadDialogueLines(string dialogueId)
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
                    var lines = sequence.Lines != null ? sequence.Lines.ToArray() : new string[0];
                    Addressables.Release(handle);
                    return lines;
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

            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            _dialogueGroup = root.AddComponent<CanvasGroup>();
            _dialogueGroup.alpha = 0f;
            _dialogueGroup.blocksRaycasts = false;
            _dialogueGroup.interactable = false;

            var panelObject = new GameObject("DialoguePanel");
            panelObject.transform.SetParent(root.transform, false);

            var panel = panelObject.AddComponent<Image>();
            panel.color = new Color(0f, 0f, 0f, 0.72f);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.06f);
            panelRect.anchorMax = new Vector2(0.92f, 0.28f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var textObject = new GameObject("DialogueText");
            textObject.transform.SetParent(panelObject.transform, false);

            _dialogueText = textObject.AddComponent<Text>();
            _dialogueText.color = Color.white;
            _dialogueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _dialogueText.fontSize = 28;
            _dialogueText.alignment = TextAnchor.MiddleLeft;
            _dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _dialogueText.verticalOverflow = VerticalWrapMode.Truncate;

            var textRect = _dialogueText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(32f, 18f);
            textRect.offsetMax = new Vector2(-32f, -18f);
        }

        private static async Task WaitForAdvance()
        {
            await Task.Yield();

            while (!UnityEngine.Input.GetMouseButtonDown(0) && !UnityEngine.Input.GetKeyDown(KeyCode.Space))
                await Task.Yield();
        }
    }
}
