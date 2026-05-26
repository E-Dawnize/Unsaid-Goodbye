using Core.Architecture;
using Core.DI;
using Gameplay.Audio;
using Gameplay.Dialogue;
using Gameplay.Ending;
using Gameplay.Interfaces;
using Gameplay.Inventory;
using Gameplay.Pause;
using Input.InputInterface;
using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(Animator))]
    [DefaultExecutionOrder(-100)]
    public class PlayerView : StrictLifecycleMonoBehaviour
    {
        [Inject] private IPlayerInput _input;
        [Inject] private IPlayerManager _manager;
        [Inject] private IDialogueManager _dialogue;
        [Inject] private IBackpackUI _backpack;
        [InjectOptional] private IPauseMenu _pauseMenu;
        [InjectOptional] private IAudioManager _audio;

        private const string FootstepSfxKey = "SFX/Footstep";
        private float _footstepTimer;

        [Header("尾巴骨骼（尾根→尾尖）")]
        [SerializeField] private Transform[] _tailBones;
        [SerializeField] private float _tailAmplitude = 160f;
        [SerializeField] private float _tailFrequency = 1.5f;
        [SerializeField] private float _tailPhasePerBone = 0.3f;
        [SerializeField] private float _tailSmoothSpeed = 12f;
        [SerializeField] [Range(0f, 1f)] private float _tailFlickChance = 0.04f;

        [Header("移动动画混合")]
        [SerializeField] private float _moveBlendSpeed = 6f;

        private Animator _animator;
        private Vector3 _originalPosition;
        private int _moveSpeedHash;
        private float _smoothedMoveSpeed;
        private float _lastFacingSign = 1f;

        // 尾巴状态 — 自己维护旋转，不依赖 Animator 给的骨骼朝向
        private float _tailSeed;
        private float[] _tailTargetAngles;
        private Quaternion[] _tailCurrent;
        private Quaternion[] _tailRestRotations;
        private bool _tailRestCaptured;
        private float _tailFlickTimer;
        private float _tailFlickDuration;
        private float _tailFlickDurationTotal;
        private float _tailFlickTarget;

        private static readonly int MoveSpeedDefault = Animator.StringToHash("MoveSpeed");

        protected override void OnInitialize()
        {
            _originalPosition = transform.position;
            _animator = GetComponent<Animator>();

            _moveSpeedHash = MoveSpeedDefault;

            _tailSeed = Random.Range(0f, 100f);
            _tailTargetAngles = new float[_tailBones.Length];
            _tailCurrent = new Quaternion[_tailBones.Length];
            _tailRestRotations = new Quaternion[_tailBones.Length];
        }

        protected override void OnStartExternal()
        {
            _input.Enable();
            _manager.SetPosition(transform.position);

            var walkableArea = FindFirstObjectByType<WalkableArea>();
            if (walkableArea != null)
                _manager.SetWalkableArea(walkableArea);
        }

        /// <summary>供摇杆等外部模块读取的输入屏蔽状态</summary>
        public static bool IsInputBlocked { get; private set; }

        protected override void Tick(float deltaTime)
        {
            var blocked = _dialogue.IsPlaying || (_backpack != null && _backpack.IsOpen) || (_pauseMenu != null && _pauseMenu.IsOpen) || EndingDirector.IsAnimating;
            IsInputBlocked = blocked;
            var direction = blocked ? Vector2.zero : _input.MoveDirection;

            _manager.Move(direction, deltaTime);

            if (direction != Vector2.zero)
            {
                transform.position = _manager.Position;
                _lastFacingSign = Mathf.Sign(direction.x);

                // 脚步声（约 0.4s 间隔）
                _footstepTimer -= deltaTime;
                if (_footstepTimer <= 0f && _audio != null)
                {
                    _footstepTimer = 0.4f;
                    _audio.PlaySfx(FootstepSfxKey);
                }
            }
            else
            {
                _footstepTimer = 0f;
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

            var targetSpeed = _manager.IsMoving ? 1f : 0f;
            _smoothedMoveSpeed = Mathf.Lerp(_smoothedMoveSpeed, targetSpeed, _moveBlendSpeed * Time.deltaTime);
            _animator.SetFloat(_moveSpeedHash, _smoothedMoveSpeed);

            var sign = direction.x != 0f ? Mathf.Sign(direction.x) : _lastFacingSign;
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * -sign;
            transform.localScale = scale;
        }

        private void LateUpdate()
        {
            if (_tailBones == null || _tailBones.Length == 0) return;

            if (!_tailRestCaptured)
            {
                for (var i = 0; i < _tailBones.Length; i++)
                {
                    if (_tailBones[i] != null)
                    {
                        _tailRestRotations[i] = _tailBones[i].localRotation;
                        _tailCurrent[i] = _tailBones[i].localRotation;
                    }
                }
                _tailRestCaptured = true;
            }

            _tailFlickTimer -= Time.deltaTime;
            if (_tailFlickTimer <= 0f && Random.value < _tailFlickChance)
            {
                _tailFlickTimer = Random.Range(1.5f, 4f);
                _tailFlickDurationTotal = Random.Range(0.2f, 0.4f);
                _tailFlickDuration = _tailFlickDurationTotal;
                _tailFlickTarget = Random.Range(-1f, 1f) * _tailAmplitude * 0.5f;
            }

            var t = Time.time;
            var freqMod = Mathf.PerlinNoise(t * 0.2f + _tailSeed, 10f) * 0.3f + 0.85f;
            var ampMod = Mathf.PerlinNoise(t * 0.15f + _tailSeed, 20f) * 0.2f + 0.9f;

            for (var i = 0; i < _tailBones.Length; i++)
            {
                var bone = _tailBones[i];
                if (bone == null) continue;

                var progress = (float)i / (_tailBones.Length - 1);
                var phase = t * _tailFrequency * freqMod - i * _tailPhasePerBone;

                var curve = Mathf.Lerp(0.08f, 1f, Mathf.Pow(progress, 0.4f));
                var main = Mathf.Sin(phase) * _tailAmplitude * ampMod * curve;

                var jitter = (Mathf.PerlinNoise(t * 1.2f + i * 0.3f, _tailSeed) * 2f - 1f)
                           * _tailAmplitude * 0.04f * progress;

                // flick 带攻入衰减包络，不再突然甩
                var flickEnv = 0f;
                if (_tailFlickDuration > 0f)
                {
                    var elapsed = _tailFlickDurationTotal - _tailFlickDuration;
                    var attack = Mathf.Clamp01(elapsed / 0.05f);
                    var decay = 1f - elapsed / _tailFlickDurationTotal;
                    flickEnv = attack * decay;
                }
                var flick = flickEnv * _tailFlickTarget * Mathf.Pow(progress, 3f);

                _tailTargetAngles[i] = main + jitter + flick;
            }

            if (_tailFlickDuration > 0f)
                _tailFlickDuration -= Time.deltaTime;

            // 自维护旋转，不依赖 Animator 给的骨骼朝向
            var step = 1f - Mathf.Exp(-_tailSmoothSpeed * Time.deltaTime);
            for (var i = 0; i < _tailBones.Length; i++)
            {
                var bone = _tailBones[i];
                if (bone == null) continue;

                var target = _tailRestRotations[i] * Quaternion.Euler(0f, 0f, _tailTargetAngles[i]);
                _tailCurrent[i] = Quaternion.Slerp(_tailCurrent[i], target, step);
                bone.localRotation = _tailCurrent[i];
            }
        }
    }
}
