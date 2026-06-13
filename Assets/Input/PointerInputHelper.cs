using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    /// <summary>
    /// 跨平台指针输入辅助 — 统一 Mouse / Pointer / Touchscreen 设备轮询。
    ///
    /// H5/WebGL 兼容要点：
    /// - Pointer 设备作为 Mouse 和 Touchscreen 的跨平台抽象回退
    /// - 所有设备 null 时返回零值/false 而不是放弃，确保设备延迟创建时后续帧能正常工作
    /// - 若此层仍未检测到输入，PlayerInputManager 中的 InputAction 绑定提供最终回退
    ///
    /// 用法：
    ///   Vector2 pos = PointerInputHelper.ScreenPosition;
    ///   if (PointerInputHelper.WasClickedThisFrame) { ... }
    ///   Vector2 world = PointerInputHelper.ScreenToWorld(pos);
    /// </summary>
    public static class PointerInputHelper
    {
        private static Camera _cachedCamera;

        /// <summary>
        /// 当前指针屏幕坐标。
        /// 优先级：Pointer（跨平台抽象，WebGL 上最可靠） → Mouse → Touchscreen（仅非零值时） → zero
        /// Touchscreen 放在最后且仅在有非零值时才用，因为 WebGL 上 EnhancedTouch 可能创建
        /// 空壳设备（无事件灌入），放前面会拦截 Pointer/Mouse 的正常坐标。
        /// </summary>
        public static Vector2 ScreenPosition
        {
            get
            {
                // Pointer 优先：WebGL 上统一处理鼠标和触摸事件
                var pointer = Pointer.current;
                if (pointer != null)
                {
                    var pos = pointer.position.ReadValue();
                    if (pos != Vector2.zero) return pos;
                }

                var mouse = Mouse.current;
                if (mouse != null)
                {
                    var pos = mouse.position.ReadValue();
                    if (pos != Vector2.zero) return pos;
                }

                // Touchscreen 兜底（仅在报告非零坐标时使用，避免空壳设备拦截）
                var touch = Touchscreen.current;
                if (touch != null)
                {
                    var pos = touch.primaryTouch.position.ReadValue();
                    if (pos != Vector2.zero) return pos;
                }

                return Vector2.zero;
            }
        }

        /// <summary>
        /// 当前帧是否触发点击。检测所有设备，任一触发即返回 true。
        /// </summary>
        public static bool WasClickedThisFrame
        {
            get
            {
                if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
                    return true;

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    return true;

                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// 当前帧指针是否处于按下/按住状态。检测所有设备，任一按下即返回 true。
        /// </summary>
        public static bool IsPressed
        {
            get
            {
                var pointer = Pointer.current;
                if (pointer != null && pointer.press.isPressed) return true;

                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed) return true;

                var touch = Touchscreen.current;
                if (touch != null && touch.primaryTouch.press.isPressed) return true;

                return false;
            }
        }

        /// <summary>
        /// 获取主摄像机（自动刷新，场景切换后自动更新引用）。
        /// 性能：仅在 Camera.main 失效时重新查找。
        /// </summary>
        public static Camera MainCamera
        {
            get
            {
                if (_cachedCamera == null || !_cachedCamera.gameObject.activeInHierarchy)
                    _cachedCamera = Camera.main;
                return _cachedCamera;
            }
        }

        /// <summary>屏幕坐标 → 世界坐标（使用当前主摄像机）</summary>
        public static Vector2 ScreenToWorld(Vector2 screenPos)
        {
            var cam = MainCamera;
            if (cam == null) return Vector2.zero;
            return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        }

        /// <summary>检查世界坐标点是否在 Collider2D 范围内</summary>
        public static bool OverlapsPoint(Collider2D collider, Vector2 worldPos)
        {
            return collider != null && collider.OverlapPoint(worldPos);
        }
    }
}
