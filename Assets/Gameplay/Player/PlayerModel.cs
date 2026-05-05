using MVVM.Model;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerModel : ModelBase
    {
        private Vector2 _direction;
        public Vector2 Direction
        {
            get => _direction;
            set
            {
                if (_direction == value) return;
                _direction = value;
                OnChanged();
            }
        }

        private float _speed;
        public float Speed
        {
            get => _speed;
            set
            {
                if (Mathf.Approximately(_speed, value)) return;
                _speed = value;
                OnChanged();
            }
        }

        private Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            set
            {
                if (_position == value) return;
                _position = value;
                OnChanged();
            }
        }

        public PlayerModel(float initSpeed, Vector3 initPosition)
        {
            Direction = Vector2.zero;
            Speed = initSpeed;
            Position = initPosition;
        }

        /// <summary>Manager 调用：应用移动输入，计算新位置</summary>
        public void ApplyMovement(Vector2 direction, float deltaTime)
        {
            Direction = direction;
            Position += new Vector3(direction.x * Speed * deltaTime, direction.y * Speed * deltaTime, 0);
        }
    }
}
