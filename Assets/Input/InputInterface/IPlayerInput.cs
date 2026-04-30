using System;
using UnityEngine;

namespace Input.InputInterface
{
    public interface IPlayerInput
    {
        event Action<Vector2> OnClickPerformed;
        event Action<Vector2>  OnMovePerformed;
        event Action<Vector2> OnMoveCanceled;
        
        void OnEnable();
        void OnDisable();
    }
}