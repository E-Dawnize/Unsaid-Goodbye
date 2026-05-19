using Core.Architecture;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
using Gameplay.Dialogue;
using Input.InputInterface;
using UnityEngine;

namespace Gameplay.Interactions
{
    /// <summary>
    /// 挂到场景物体上，通过 Collider2D 检测交互，将事件发布到 IEventCenter。
    /// 零耦合，只发布 InteractionEvent，各系统自行从 InteractionDef 读取所需数据。
    ///
    /// </summary>
    public enum InteractMode
    {
        Click,      // 点击触发
        Trigger,    // 进入触发区域自动触发
        Proximity   // 进入范围后可点击触发
    }

    [RequireComponent(typeof(Collider2D))]
    public class InteractableObject : StrictLifecycleMonoBehaviour
    {
        [Inject] private IEventCenter _events;
        [Inject] private IPlayerInput _input;
        [Inject] private IDialogueManager _dialogue;

        [Header("交互定义")]
        [SerializeField] private InteractionDef _def;

        [Header("交互模式")]
        [SerializeField] private InteractMode _mode = InteractMode.Click;
        [SerializeField] private bool _interactOnce = true;
        [SerializeField] private string _playerTag = "Player";

        [Header("场景切换（Trigger 模式下可选）")]
        [Tooltip("触发后加载的目标场景路径，如 Scenes/Balcony")]
        [SerializeField] private string _sceneToLoad;

        private bool _playerInRange;

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
        protected override void Tick(float deltaTime)
        {
            if (_mode == InteractMode.Proximity && _playerInRange && _dialogue != null && _input != null && _input.IsClickTriggered && !_dialogue.IsPlaying)
                Fire();
        }

        private void OnMouseDown()
        {
            if (_mode == InteractMode.Click)
                Fire();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(_playerTag)) return;

            switch (_mode)
            {
                case InteractMode.Trigger:
                    Fire();
                    break;
                case InteractMode.Proximity:
                    _playerInRange = true;
                    SetHover(true);
                    break;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag(_playerTag)) return;
            if (_mode == InteractMode.Proximity)
            {
                _playerInRange = false;
                SetHover(false);
            }
        }
        #endregion

        #region 视觉反馈
        private void OnMouseEnter()
        {
            if (_mode != InteractMode.Click || !_enableHoverEffect || _sprite == null) return;
            SetHover(true);
        }

        private void OnMouseExit()
        {
            if (_mode != InteractMode.Click || !_enableHoverEffect || _sprite == null) return;
            SetHover(false);
        }

        private void SetHover(bool hover)
        {
            if (_sprite != null)
                _sprite.color = hover ? _hoverColor : _defaultColor;
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

            if (!string.IsNullOrEmpty(_sceneToLoad))
            {
                if (_sceneToLoad.StartsWith("Scenes/"))
                    _events.Publish(new SceneLoadRequest { ScenePath = _sceneToLoad });
                else
                    Debug.LogWarning($"[Interactable] Invalid scene path '{_sceneToLoad}' on '{_def.name}'. Expected format: Scenes/XXX", this);
            }

            Debug.Log($"[Interactable] '{_def.name}' fired");

            if (effectiveOnce)
                SetHover(false);
        }
        #endregion
    }
}
