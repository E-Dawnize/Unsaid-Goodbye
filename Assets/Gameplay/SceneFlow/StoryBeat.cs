using Core.Identity;
using UnityEngine;

namespace Gameplay.SceneFlow
{
    [CreateAssetMenu(fileName = "StoryBeat", menuName = "Gameplay/Story Beat")]
    public class StoryBeat : ScriptableObject
    {
        public InteractionDef Def;

        [TextArea(1, 2)]
        public string Description;
    }
}
