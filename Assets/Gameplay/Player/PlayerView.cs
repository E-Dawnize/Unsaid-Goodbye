using Core.Architecture;
using Core.DI;
using Gameplay.Interfaces;
using Input.InputInterface;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Player View — 挂在玩家 GameObject 上
    /// 每帧从 IPlayerInput 轮询输入 → 写入 Manager → 同步 Transform
    /// </summary>
    public class PlayerView : StrictLifecycleMonoBehaviour
    {
        [Inject] private IPlayerInput _input;
        [Inject] private IPlayerManager _manager;

        protected override void OnStartExternal()
        {
            _input.Enable();
            transform.position = _manager.Position;
        }

        protected override void Tick(float deltaTime)
        {
            var direction = _input.MoveDirection;
            if (direction != Vector2.zero)
            {
                _manager.Move(direction, deltaTime);
                transform.position = _manager.Position;
            }
        }

        protected override void OnShutdown()
        {
            _input?.Disable();
        }
    }
}
