using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.SO
{
    [CreateAssetMenu(fileName = "DialogueSequence", menuName = "SO/DialogueSequence")]
    public class DialogueSequence : ScriptableObject
    {
        public string DialogueId;

        [TextArea(2, 4)]
        public List<string> Lines = new();
    }
}
