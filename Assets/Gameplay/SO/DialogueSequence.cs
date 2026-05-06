using System.Collections.Generic;
using Core.Identity;
using UnityEngine;

namespace Gameplay.SO
{
    public enum DialogueLineType
    {
        Narration,
        InnerMonologue,
        Interaction,
        StoryText
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string Text;
        public string ResultDialogueId;
    }

    [System.Serializable]
    public class DialogueEntry
    {
        public DialogueLineType Type;
        public string Speaker;

        [TextArea(2, 4)]
        public string Text;

        public Color TextColor = Color.clear;
        public bool AutoAdvance;
        public float AutoAdvanceDelay = 1.5f;
        public List<DialogueChoice> Choices = new();
    }

    [CreateAssetMenu(fileName = "DialogueSequence", menuName = "SO/DialogueSequence")]
    public class DialogueSequence : ScriptableObject
    {
        public string DialogueId;
        public InteractableId CompletionId;

        [TextArea(2, 4)]
        public List<string> Lines = new();

        public List<DialogueEntry> Entries = new();
    }
}
