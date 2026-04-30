using Core.Architecture;
using Core.DI;
using Core.Events.EventInterfaces;
using UnityEngine;

namespace Gameplay.Interactions
{
    /// <summary>
    /// 挂到场景物体上，通过 Collider2D 检测交互，将事件发布到 IEventCenter。
    /// 零耦合：不引用 Controller、View 或任何特定业务系统。
    ///
    /// 两种模式：
    ///   - 点击交互：挂 Collider2D（非 Trigger），玩家点击时 OnMouseDown → 发布事件
    ///   - 触发区域：挂 Collider2D（IsTrigger），玩家进入时 OnTriggerEnter2D → 发布事件
    /// </summary>
    public enum InteractableType
    {
        Collectible,   // → ItemCollectedEvent { ItemID }
        Puzzle,        // → PuzzleSolvedEvent { PuzzleID }
        Interactive,   // → InteractionPerformedEvent { InteractableID }
        TriggerZone    // → TriggerEnterEvent { TriggerID }
    }

    [RequireComponent(typeof(Collider2D))]
    public class InteractableObject : StrictLifecycleMonoBehaviour
    {
        [Inject] private IEventCenter _events;

        [Header("交互定义")]
        [SerializeField] private InteractableType _type;
        [SerializeField] private string _targetId;

        [Header("交互模式")]
        [Tooltip("true=仅作触发区域(OnTriggerEnter)；false=点击交互(OnMouseDown)")]
        [SerializeField] private bool _triggerOnly;
        [SerializeField] private bool _interactOnce = true;
        [SerializeField] private string _playerTag = "Player";

        [Header("视觉反馈（点击交互模式下生效）")]
        [SerializeField] private bool _enableHoverEffect = true;
        [SerializeField] private Color _hoverColor = new(1f, 1f, 0.8f, 1f);

        private SpriteRenderer _sprite;
        private Color _defaultColor;
        private bool _used;

        #region Lifecycle
        protected override void OnInitialize()
        {
            if (_enableHoverEffect)
                _sprite = GetComponent<SpriteRenderer>();

            _defaultColor = _sprite != null ? _sprite.color : Color.white;
        }
        #endregion

        #region 交互入口
        private void OnMouseDown()
        {
            if (_triggerOnly) return;
            Fire();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_triggerOnly) return;
            if (!other.CompareTag(_playerTag)) return;
            Fire();
        }
        #endregion

        #region 视觉反馈（点击模式）
        private void OnMouseEnter()
        {
            if (_triggerOnly || !_enableHoverEffect || _sprite == null) return;
            _sprite.color = _hoverColor;
        }

        private void OnMouseExit()
        {
            if (_triggerOnly || !_enableHoverEffect || _sprite == null) return;
            _sprite.color = _defaultColor;
        }
        #endregion

        #region 事件发布
        private void Fire()
        {
            if (_used && _interactOnce) return;

            if (_events == null)
            {
                Debug.LogWarning(
                    $"[Interactable] IEventCenter not injected. " +
                    $"Ensure DI container is set up before interaction. " +
                    $"Object: '{_targetId}' ({_type})", this);
                return;
            }

            _used = true;

            switch (_type)
            {
                case InteractableType.Collectible:
                    _events.Publish(new ItemCollectedEvent { ItemID = _targetId });
                    break;
                case InteractableType.Puzzle:
                    _events.Publish(new PuzzleSolvedEvent { PuzzleID = _targetId });
                    break;
                case InteractableType.Interactive:
                    _events.Publish(new InteractionPerformedEvent { InteractableID = _targetId });
                    break;
                case InteractableType.TriggerZone:
                    _events.Publish(new TriggerEnterEvent { TriggerID = _targetId });
                    break;
            }

            Debug.Log($"[Interactable] {_type} '{_targetId}' fired");

            if (_interactOnce)
                ResetHover();
        }

        private void ResetHover()
        {
            if (_sprite != null)
                _sprite.color = _defaultColor;
        }
        #endregion
    }
}
