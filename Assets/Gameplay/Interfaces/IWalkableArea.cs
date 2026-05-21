using UnityEngine;

namespace Gameplay.Interfaces
{
    public interface IWalkableArea
    {
        bool Contains(Vector2 point);
        Vector3 ClampToArea(Vector3 point);
    }
}
