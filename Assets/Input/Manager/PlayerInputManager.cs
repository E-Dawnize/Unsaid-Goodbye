using Input.InputConfig;
using Input.InputInterface;
using UnityEngine;

namespace Input.Manager
{
    /// <summary>
    /// 玩家输入实现 — 基于新 Input System 轮询 InputAction 当前值。
    /// 虚拟摇杆作为补充输入，与键盘方向共存；不使用旧输入系统。
    /// </summary>
    public class PlayerInputManager : IPlayerInput
    {
        private readonly PlayerInputActions _actions;
        private bool _enabled;

        public Vector2 MoveDirection => _enabled
            ? CombineMoveDirection(_actions.Gameplay.Move.ReadValue<Vector2>(), VirtualJoystickInput.Direction)
            : Vector2.zero;

        public Vector2 MousePosition => _enabled
            ? _actions.Gameplay.MousePosition.ReadValue<Vector2>()
            : Vector2.zero;

        public bool IsClickTriggered => _enabled
            && _actions.Gameplay.Click.WasPressedThisFrame();

        public bool BackpackToggleTriggered => _enabled
            && UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame;

        public PlayerInputManager()
        {
            _actions = new PlayerInputActions();
        }

        public void Enable()
        {
            _actions.Gameplay.Enable();
            _enabled = true;
        }

        public void Disable()
        {
            _actions.Gameplay.Disable();
            _enabled = false;
            VirtualJoystickInput.SetDirection(Vector2.zero);
        }

        private static Vector2 CombineMoveDirection(Vector2 inputActionDirection, Vector2 virtualDirection)
        {
            if (virtualDirection.sqrMagnitude > 0.0001f)
                return Vector2.ClampMagnitude(virtualDirection, 1f);

            return Vector2.ClampMagnitude(inputActionDirection, 1f);
        }
    }
}
