using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerManager : IPlayerManager
    {
        private readonly PlayerModel _model;
        private IWalkableArea _walkableArea;

        public Vector3 Position => _model.Position;
        public Vector2 Direction => _model.Direction;
        public bool IsMoving => _model.IsMoving;

        public PlayerManager()
        {
            _model = new PlayerModel(initSpeed: 3f, initPosition: Vector3.zero);
        }

        public void SetPosition(Vector3 position)
        {
            _model.SetPosition(position);
        }

        public void SetWalkableArea(IWalkableArea area)
        {
            _walkableArea = area;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _model.Speed = 3f * multiplier;
        }

        public void Move(Vector2 direction, float deltaTime)
        {
            _model.ApplyMovement(direction, deltaTime);
            if (_walkableArea != null)
                _model.Position = _walkableArea.ClampToArea(_model.Position);
        }
    }
}
