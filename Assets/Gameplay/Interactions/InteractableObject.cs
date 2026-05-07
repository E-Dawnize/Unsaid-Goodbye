using Core.Architecture;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
using UnityEngine;

namespace Gameplay.Interactions
{
    /// <summary>
    /// 挂到场景物体上，通过 Collider2D 检测交互，将事件发布到 IEventCenter。
    /// 零耦合，只发布 InteractionEvent，各系统自行从 InteractionDef 读取所需数据。
    ///
    /// 两种模式：
    ///   - 点击交互：挂 Collider2D（非 Trigger），玩家点击时 OnMouseDown → 发布事件
    ///   - 触发区域：挂 Collider2D（IsTrigger），玩家进入时 OnTriggerEnter2D → 发布事件
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InteractableObject : StrictLifecycleMonoBehaviour
    {
        [Inject] private IEventCenter _events;

        [Header("交互定义")]
        [SerializeField] private InteractionDef _def;

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
                    $"Object: '{_def}'", this);
                return;
            }

            if (_def == null)
            {
                Debug.LogWarning($"[Interactable] InteractionDef is null on '{gameObject.name}'", this);
                return;
            }

            var effectiveOnce = _def.OneShot && _interactOnce;
            if (_used && effectiveOnce) return;

            _used = true;

            _events.Publish(new InteractionEvent { Def = _def });

            Debug.Log($"[Interactable] '{_def.name}' fired");

            if (effectiveOnce)
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
