using System;
using Gameplay.Ending;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Settings
{
    /// <summary>
    /// 设置面板中的可点击按钮——挂到 sprite 子物体上，点击即触发回调。
    /// </summary>
    public class SettingsButton : MonoBehaviour
    {
        public event Action OnClick;

        private Camera _cam;
        private SpriteRenderer _sr;
        private Color _origColor;
        private bool _hovered;
        private bool _pressed;

        private void Start()
        {
            _cam = Camera.main;
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _origColor = _sr.color;
            if (GetComponent<Collider2D>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                if (_sr != null && _sr.sprite != null)
                    col.size = _sr.sprite.bounds.size;
            }
            gameObject.layer = LayerMask.NameToLayer("UI");
            Debug.Log($"[SettingsButton] Start on '{name}', sr={_sr != null}, col={GetComponent<Collider2D>() != null}, layer={gameObject.layer}");
        }

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;
            // 结局动画期间屏蔽，但不屏蔽暂停菜单
            if (_cam == null || EndingDirector.IsAnimating) return;

            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            Vector2 screenPos;
            bool isDown;
            bool clicked;

            if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
                isDown = mouse.leftButton.isPressed;
                clicked = mouse.leftButton.wasPressedThisFrame;
            }
            else if (touch != null)
            {
                screenPos = touch.primaryTouch.position.ReadValue();
                isDown = touch.primaryTouch.press.isPressed;
                clicked = touch.primaryTouch.press.wasPressedThisFrame;
            }
            else return;

            var worldPos = (Vector2)_cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
            var col = GetComponent<Collider2D>();
            var isOver = col != null && col.OverlapPoint(worldPos);

            if (isOver && isDown) _pressed = true;

            // 颜色反馈：按下 0.6，悬停 0.8
            if (_sr != null)
            {
                if (_pressed && isDown)
                    _sr.color = new Color(_origColor.r * 0.6f, _origColor.g * 0.6f, _origColor.b * 0.6f, _origColor.a);
                else if (isOver)
                    _sr.color = new Color(_origColor.r * 0.8f, _origColor.g * 0.8f, _origColor.b * 0.8f, _origColor.a);
                else
                    _sr.color = _origColor;
            }

            if (isOver && clicked && _pressed)
            {
                _pressed = false;
                OnClick?.Invoke();
            }

            if (!isDown) _pressed = false;
        }
    }
}
