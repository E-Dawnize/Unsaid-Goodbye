using System.Collections.Generic;
using Core.Architecture;
using Core.DI;
using Core.Events.EventInterfaces;
using Core.Identity;
using Gameplay.Audio;
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
        [Inject] private IAudioManager _audio;
        [InjectOptional] private Inventory.IInventoryManager _inventory;

        [Header("交互定义")]
        [SerializeField] private InteractionDef _def;
        public InteractionDef Def => _def;

        [Header("交互模式")]
        [SerializeField] private InteractMode _mode = InteractMode.Click;
        [SerializeField] private bool _interactOnce = true;
        [SerializeField] private string _playerTag = "Player";

        [Header("场景切换（Trigger 模式下可选）")]
        [Tooltip("触发后加载的目标场景路径，如 Scenes/3.Surface_Balcony")]
        [SerializeField] private string _sceneToLoad;

        [Header("前置条件")]
        [Tooltip("需要先触发此交互物后才能交互")]
        [SerializeField] private InteractionDef _prerequisite;

        [Header("交互后激活")]
        [Tooltip("交互触发后需要显示的场景物体")]
        [SerializeField] private GameObject[] _targetsToActivateOnInteract;

        [Header("交互后隐藏")]
        [Tooltip("交互触发后需要隐藏的场景物体，例如被拾取的道具实体。")]
        [SerializeField] private GameObject[] _targetsToHideOnInteract;

        private bool _playerInRange;

        /// <summary>
        /// 当前玩家范围内所有 Proximity 模式交互物，供 InteractionPromptView 多图标显示。
        /// </summary>
        public static readonly List<InteractableObject> ProximityTargets = new();

        /// <summary>是否仍可交互（未被一次性消耗 + 前置条件满足）</summary>
        public bool CanInteract
        {
            get
            {
                if (_used && _def != null && _def.OneShot && _interactOnce) return false;
                if (_prerequisite != null && _inventory != null)
                {
                    foreach (var item in _inventory.CollectedItems)
                        if (item == _prerequisite) return true;
                    return false; // 前置未收集
                }
                return true;
            }
        }

        /// <summary>读档时标记为已使用，防止一次性交互物被重复触发</summary>
        public void MarkUsed()
        {
            _used = true;
            SetHover(false);
            ProximityTargets.Remove(this);
            HideTargetsAfterInteract();
            ActivateTargetsAfterInteract();
        }

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
        // Proximity 模式的点击交互已迁移到 InteractionPromptView，
        // Tick 保留以便后续扩展其他每帧逻辑。

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
                    if (CanInteract)
                    {
                        SetHover(true);
                        if (!ProximityTargets.Contains(this))
                            ProximityTargets.Add(this);
                    }
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
                ProximityTargets.Remove(this);
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
        public void Fire()
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

            // 一次性交互：消耗后立即从列表移除
            if (effectiveOnce)
                ProximityTargets.Remove(this);

            _events.Publish(new InteractionEvent { Def = _def });

            // 播放交互音效
            if (!string.IsNullOrEmpty(_def.SfxKey))
                _audio?.PlaySfx(_def.SfxKey);

            if (!string.IsNullOrEmpty(_sceneToLoad))
            {
                if (_sceneToLoad.StartsWith("Scenes/"))
                    _events.Publish(new SceneLoadRequest { ScenePath = _sceneToLoad });
                else
                    Debug.LogWarning($"[Interactable] Invalid scene path '{_sceneToLoad}' on '{_def.name}'. Expected format: Scenes/XXX", this);
            }

            Debug.Log($"[Interactable] '{_def.name}' fired");

            if (effectiveOnce)
            {
                HideTargetsAfterInteract();
                ActivateTargetsAfterInteract();
                SetHover(false);
            }
        }

        private void ActivateTargetsAfterInteract()
        {
            if (_targetsToActivateOnInteract == null) return;
            foreach (var target in _targetsToActivateOnInteract)
                if (target != null) target.SetActive(true);
        }

        private void HideTargetsAfterInteract()
        {
            if (_targetsToHideOnInteract == null)
                return;

            foreach (var target in _targetsToHideOnInteract)
            {
                if (target != null)
                    target.SetActive(false);
            }
        }
        #endregion
    }
}
