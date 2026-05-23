using UnityEngine;

namespace Input.InputInterface
{
    /// <summary>
    /// 玩家输入接口 — 基于轮询，供 PlayerView 每帧读取。
    /// 离散事件（如点击）由 InputAction.WasPressedThisFrame 直接判断。
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>当前帧移动方向，键盘和虚拟摇杆共存。</summary>
        Vector2 MoveDirection { get; }

        /// <summary>当前鼠标位置。</summary>
        Vector2 MousePosition { get; }

        /// <summary>当前帧是否点击。</summary>
        bool IsClickTriggered { get; }

        void Enable();
        void Disable();
    }
}
