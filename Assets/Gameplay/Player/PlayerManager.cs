using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerManager : IPlayerManager
    {
        private readonly PlayerModel _model;

        public Vector3 Position => _model.Position;
        public Vector2 Direction => _model.Direction;

        public PlayerManager()
        {
            _model = new PlayerModel(initSpeed: 5f, initPosition: Vector3.zero);
        }

        public void Move(Vector2 direction, float deltaTime)
        {
            if (direction == Vector2.zero) return;
            _model.ApplyMovement(direction, deltaTime);
        }
    }
}
