using UnityEngine;

namespace Gameplay.Interfaces
{
    public interface IPlayerManager
    {
        Vector3 Position { get; }
        Vector2 Direction { get; }
        bool IsMoving { get; }
        void SetPosition(Vector3 position);
        void Move(Vector2 direction, float deltaTime);
    }
}
