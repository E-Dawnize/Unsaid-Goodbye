using Core.Architecture;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gameplay.Rendering
{
    /// <summary>
    /// 角色排序：根据 sortingAnchor Y 坐标动态更新 SortingGroup.sortingOrder
    /// </summary>
    public class CharacterDepthSorter : StrictLifecycleMonoBehaviour
    {
        [SerializeField] private Transform _sortingAnchor;
        [SerializeField] private int _sortPrecision = 100;

        private SortingGroup _sortingGroup;

        protected override void OnInitialize()
        {
            _sortingGroup = GetComponent<SortingGroup>();
            if (_sortingAnchor == null)
                _sortingAnchor = transform;
        }

        public void SetAnchorY(float worldY)
        {
            var pos = _sortingAnchor.position;
            pos.y = worldY;
            _sortingAnchor.position = pos;
        }

        public Transform SortingAnchor => _sortingAnchor;

        private void LateUpdate()
        {
            if (_sortingGroup == null || _sortingAnchor == null) return;

            var order = -(int)(_sortingAnchor.position.y * _sortPrecision);
            _sortingGroup.sortingOrder = Mathf.Clamp(order, short.MinValue, short.MaxValue);
        }
    }
}
