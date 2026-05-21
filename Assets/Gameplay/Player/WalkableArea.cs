using Core.Architecture;
using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// 场景可行走区域，使用 PolygonCollider2D 定义边界。
    /// 通过 _insetMargin 将角色圆心内缩，避免体积穿模。
    /// </summary>
    [RequireComponent(typeof(PolygonCollider2D))]
    public class WalkableArea : StrictLifecycleMonoBehaviour, IWalkableArea
    {
        [SerializeField] private float _insetMargin = 0.5f;

        private PolygonCollider2D _collider;

        protected override void OnInitialize()
        {
            _collider = GetComponent<PolygonCollider2D>();
            _collider.isTrigger = true;
        }

        public bool Contains(Vector2 point)
        {
            var closest = _collider.ClosestPoint(point);
            return _collider.OverlapPoint(point)
                   && Vector2.Distance(point, closest) >= _insetMargin;
        }

        public Vector3 ClampToArea(Vector3 point)
        {
            var p2 = (Vector2)point;
            var closest = _collider.ClosestPoint(p2);
            var toPoint = (p2 - closest).normalized;

            if (toPoint.sqrMagnitude < 0.0001f)
                toPoint = Vector2.up;

            // 尝试两个方向推入 margin
            var candidates = new[]
            {
                closest + toPoint * _insetMargin,
                closest - toPoint * _insetMargin,
            };

            foreach (var c in candidates)
            {
                if (_collider.OverlapPoint(c) &&
                    Vector2.Distance(c, _collider.ClosestPoint(c)) >= _insetMargin - 0.001f)
                {
                    return new Vector3(c.x, c.y, point.z);
                }
            }

            return new Vector3(closest.x, closest.y, point.z);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var col = GetComponent<PolygonCollider2D>();
            if (col == null || col.pathCount == 0) return;

            // 原始边界 — 绿色
            DrawPolygon(col, Color.green);

            // 内缩边界 — 黄色（近似）
            DrawInsetPolygon(col, Color.yellow);
        }

        private static void DrawPolygon(PolygonCollider2D col, Color color)
        {
            Gizmos.color = color;
            var t = col.transform;
            for (var p = 0; p < col.pathCount; p++)
            {
                var pts = col.GetPath(p);
                for (var i = 0; i < pts.Length; i++)
                {
                    var a = t.TransformPoint(pts[i]);
                    var b = t.TransformPoint(pts[(i + 1) % pts.Length]);
                    Gizmos.DrawLine(a, b);
                }
            }
        }

        private void DrawInsetPolygon(PolygonCollider2D col, Color color)
        {
            Gizmos.color = color;
            var t = col.transform;
            for (var p = 0; p < col.pathCount; p++)
            {
                var pts = col.GetPath(p);
                if (pts.Length < 3) continue;

                var centroid = Vector2.zero;
                foreach (var pt in pts) centroid += pt;
                centroid /= pts.Length;

                var inset = new Vector3[pts.Length];
                for (var i = 0; i < pts.Length; i++)
                {
                    var dir = ((Vector2)pts[i] - centroid).normalized;
                    if (dir.sqrMagnitude < 0.0001f)
                        dir = Vector2.right;
                    inset[i] = t.TransformPoint(pts[i] + dir * _insetMargin);
                }

                for (var i = 0; i < inset.Length; i++)
                    Gizmos.DrawLine(inset[i], inset[(i + 1) % inset.Length]);
            }
        }
#endif
    }
}
