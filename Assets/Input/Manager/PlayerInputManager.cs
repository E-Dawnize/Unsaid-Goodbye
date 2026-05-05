using Input.InputInterface;
using Input.InputConfig;
using UnityEngine;

namespace Input.Manager
{
    /// <summary>
    /// 玩家输入实现 — 纯 C# 类，轮询 InputAction 当前值
    /// 不依赖 MonoBehaviour，直接注册为 Singleton
    /// </summary>
    public class PlayerInputManager : IPlayerInput
    {
        private PlayerInputActions _actions;
        private bool _enabled;

        public Vector2 MoveDirection => _enabled
            ? _actions.Gameplay.Move.ReadValue<Vector2>()
            : Vector2.zero;

        public Vector2 MousePosition => _enabled
            ? _actions.Gameplay.MousePosition.ReadValue<Vector2>()
            : Vector2.zero;

        public bool IsClickTriggered => _enabled
            && _actions.Gameplay.Click.WasPressedThisFrame();

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
        }
    }
}
