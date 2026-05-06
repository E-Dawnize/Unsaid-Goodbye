using Gameplay.Dialogue;
using Gameplay.SO;
using UnityEngine;

namespace Core.Identity
{
    /// <summary>
    /// 交互定义 — 一个 SO 捆绑交互物的全部行为数据。
    /// 场景物体挂 InteractableObject，拖入一个 InteractionDef 即可。
    /// </summary>
    [CreateAssetMenu(fileName = "Def_", menuName = "SO/InteractionDef")]
    public class InteractionDef : ScriptableObject
    {
        [TextArea(1, 3)]
        [SerializeField] private string _description;

        public DialogueSequence Dialogue;
        public bool OneShot = true;
    }
}
