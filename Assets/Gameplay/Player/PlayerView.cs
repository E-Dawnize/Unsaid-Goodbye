using Core.Architecture;
using Core.DI;
using Gameplay.Dialogue;
using Gameplay.Interfaces;
using Input.InputInterface;
using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerView : StrictLifecycleMonoBehaviour
    {
        [Inject] private IPlayerInput _input;
        [Inject] private IPlayerManager _manager;
        [Inject] private IDialogueManager _dialogue;

        private Animator _animator;
        private Vector3 _originalPosition;
        private int _isMovingHash;
        private int _velXHash;
        private int _velYHash;
        private float _lastFacingSign = 1f;

        private static readonly int IsMovingDefault = Animator.StringToHash("IsMoving");
        private static readonly int VelXDefault = Animator.StringToHash("VelX");
        private static readonly int VelYDefault = Animator.StringToHash("VelY");

        protected override void OnInitialize()
        {
            _originalPosition = transform.position;
            _animator = GetComponent<Animator>();

            _isMovingHash = IsMovingDefault;
            _velXHash = VelXDefault;
            _velYHash = VelYDefault;
        }

        protected override void OnStartExternal()
        {
            _input.Enable();
            transform.position = _manager.Position;
        }

        protected override void Tick(float deltaTime)
        {
            var direction = _dialogue.IsPlaying ? Vector2.zero : _input.MoveDirection;

            _manager.Move(direction, deltaTime);

            if (direction != Vector2.zero)
            {
                transform.position = _manager.Position;
                _lastFacingSign = Mathf.Sign(direction.x);
            }

            ApplyAnimationState(direction);
        }

        protected override void OnShutdown()
        {
            _input?.Disable();
            transform.position = _originalPosition;
        }

        private void ApplyAnimationState(Vector2 direction)
        {
            if (_animator == null) return;

            _animator.SetBool(_isMovingHash, _manager.IsMoving);
            // _animator.SetFloat(_velXHash, direction.x);
            // _animator.SetFloat(_velYHash, direction.y);

            var sign = direction.x != 0f ? Mathf.Sign(direction.x) : _lastFacingSign;
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * -sign;
            transform.localScale = scale;
        }
    }
}
