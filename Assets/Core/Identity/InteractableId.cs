using UnityEngine;

namespace Core.Identity
{
    /// <summary>
    /// 交互物标识符 — ScriptableObject 资产本身即 ID。
    /// 匹配使用引用相等（==），杜绝字符串拼写错误。
    /// 在 Inspector 中拖拽引用，可通过 Find References 查找所有使用处。
    /// </summary>
    [CreateAssetMenu(fileName = "Id_", menuName = "SO/InteractableId")]
    public class InteractableId : ScriptableObject
    {
        [TextArea(1, 3)]
        [SerializeField] private string _description;
    }
}
