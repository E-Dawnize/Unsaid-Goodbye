using Gameplay.Player;
using Input.Manager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input.UI
{
    /// <summary>
    /// 世界空间摇杆——N4 内摇杆随触摸/鼠标拖动，底圈 D1-D4 固定不动。
    /// 通过 VirtualJoystickInput 接入现有输入管线，PlayerView.Tick 中自动屏蔽。
    /// </summary>
    public class WorldSpaceJoystick : MonoBehaviour
    {
        private Transform _knob;
        private Vector3 _knobCenter;
        private float _radius;
        private int _pointerId = -1;

        public void Init(Transform knob, float radius)
        {
            _knob = knob;
            _radius = radius;
            _knobCenter = knob.localPosition;
        }

        private void Update()
        {
            if (_knob == null || PlayerView.IsInputBlocked)
            {
                VirtualJoystickInput.SetDirection(Vector2.zero);
                _pointerId = -1;
                return;
            }

            var mouse = Mouse.current;
            var touch = Touchscreen.current;

            if (mouse != null)
            {
                HandlePointer(mouse.position.ReadValue(), mouse.leftButton.isPressed);
            }
            else if (touch != null)
            {
                var t = touch.primaryTouch;
                HandlePointer(t.position.ReadValue(), t.press.isPressed);
            }
            else
            {
                ResetKnob();
            }
        }

        private void HandlePointer(Vector2 screenPos, bool isPressed)
        {
            if (isPressed)
            {
                // 新按下：检查是否在摇杆范围内
                if (_pointerId < 0)
                {
                    var worldPos = ScreenToWorld(screenPos);
                    var basePos = (Vector2)transform.TransformPoint(_knobCenter);
                    if (Vector2.Distance(worldPos, basePos) < _radius * 1.8f)
                        _pointerId = 0;
                }

                if (_pointerId >= 0)
                {
                    var worldPos = ScreenToWorld(screenPos);
                    var basePos = (Vector2)transform.TransformPoint(_knobCenter);
                    var delta = worldPos - basePos;
                    var clamped = Vector2.ClampMagnitude(delta, _radius);
                    _knob.position = (Vector2)basePos + clamped;
                    VirtualJoystickInput.SetDirection(clamped / _radius);
                }
            }
            else
            {
                ResetKnob();
            }
        }

        private void ResetKnob()
        {
            _pointerId = -1;
            _knob.localPosition = _knobCenter;
            VirtualJoystickInput.SetDirection(Vector2.zero);
        }

        private static Vector2 ScreenToWorld(Vector2 screenPos)
        {
            var cam = Camera.main;
            if (cam == null) return Vector2.zero;
            return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        }

        private void OnDisable()
        {
            VirtualJoystickInput.SetDirection(Vector2.zero);
        }
    }
}
