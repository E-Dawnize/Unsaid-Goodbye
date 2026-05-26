using Core.Architecture;
using UnityEngine;

namespace Gameplay.Rendering
{
    /// <summary>
    /// 场景物体排序：根据排序锚点 Y 坐标动态更新 SpriteRenderer.sortingOrder
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DepthSortable : StrictLifecycleMonoBehaviour
    {
        [SerializeField] private Transform _sortingAnchor;
        [SerializeField] private float _orderOffset;
        [SerializeField] private int _sortPrecision = 100;

        private SpriteRenderer _sprite;

        protected override void OnInitialize()
        {
            _sprite = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (_sprite == null) return;

            var anchorY = _sortingAnchor != null
                ? _sortingAnchor.position.y
                : _sprite.bounds.min.y;

            var order = -(int)(anchorY * _sortPrecision) + (int)_orderOffset;
            _sprite.sortingOrder = Mathf.Clamp(order, short.MinValue, short.MaxValue);
        }
    }
}
