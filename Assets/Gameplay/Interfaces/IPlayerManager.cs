using UnityEngine;

namespace Gameplay.Interfaces
{
    public interface IPlayerManager
    {
        Vector3 Position { get; }
        Vector2 Direction { get; }
        void Move(Vector2 direction, float deltaTime);
    }
}
