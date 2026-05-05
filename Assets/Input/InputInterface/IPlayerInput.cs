using UnityEngine;

namespace Input.InputInterface
{
    /// <summary>
    /// 玩家输入接口 — 基于轮询（polling），供 PlayerView 每帧 Update 读取
    /// 离散事件（如点击）由 InputAction.WasPressedThisFrame 直接判断
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>当前帧移动方向 (WASD)，未按下时返回 Vector2.zero</summary>
        Vector2 MoveDirection { get; }

        /// <summary>当前鼠标位置</summary>
        Vector2 MousePosition { get; }

        /// <summary>当前帧是否点击（鼠标左键按下）</summary>
        bool IsClickTriggered { get; }

        void Enable();
        void Disable();
    }
}
