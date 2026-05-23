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

        /// <summary>
        /// Proximity 模式下玩家靠近时显示的交互提示图标 Addressables Key。
        /// 留空则不显示图标。不同交互物可配置不同图标路径。
        /// </summary>
        [Tooltip("Proximity 模式下显示的交互提示图 Addressables Key（留空不显示）")]
        public string PromptIconKey;

        /// <summary>交互触发时播放的音效 Addressables Key，留空则不播放</summary>
        [Tooltip("交互时播放的音效 Addressables Key")]
        public string SfxKey;
    }
}
