using System;
using Gameplay.Ending;
using UnityEngine;
using Input;
using UnityEngine.InputSystem;

namespace Gameplay.Settings
{
    /// <summary>
    /// 世界空间滑动条——拖动子物体 sprite 在指定 X 范围内滑动，输出 0~1 值。
    /// 挂在滑块手柄上，parent 为轨道基准。
    /// </summary>
    public class WorldSpaceSlider : MonoBehaviour
    {
        [SerializeField] private float _minX = -0.5f;
        [SerializeField] private float _maxX = 2.5f;
        [SerializeField] private Vector2 _hitOffset = new(0f, -0.09f);
        [SerializeField] private float _hitRadius = 0.5f;

        public float Value { get; private set; }
        public event Action<float> OnValueChanged;

        private Vector3 _basePos;
        private bool _dragging;
        private Camera _cam;

        /// <summary>直接设初始值（0~1）并移动滑块到对应世界 X 位置</summary>
        public void InitValue(float t)
        {
            _cam = Camera.main;
            _basePos = transform.position;
            Value = Mathf.Clamp01(t);
            var x = Mathf.Lerp(_minX, _maxX, Value);
            transform.position = new Vector3(x, _basePos.y, transform.position.z);
        }

        private void Start()
        {
            if (_basePos == Vector3.zero)
            {
                _cam = Camera.main;
                _basePos = transform.position;
            }
        }

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null || EndingDirector.IsAnimating) return;

            var screenPos = PointerInputHelper.ScreenPosition;
            var pressed = PointerInputHelper.IsPressed;

            if (pressed)
            {
                var worldPos = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
                var hitCenter = (Vector2)transform.position + _hitOffset;
                if (!_dragging && Vector2.Distance(worldPos, hitCenter) < _hitRadius)
                    _dragging = true;

                if (_dragging)
                {
                    var t = Mathf.InverseLerp(_minX, _maxX, worldPos.x);
                    SetValue(t);
                }
            }
            else
            {
                _dragging = false;
            }
        }

        public void SetValue(float t)
        {
            var clamped = Mathf.Clamp01(t);
            if (Mathf.Approximately(Value, clamped)) return;
            Value = clamped;
            UpdateHandlePosition();
            OnValueChanged?.Invoke(Value);
        }

        private void UpdateHandlePosition()
        {
            var x = Mathf.Lerp(_minX, _maxX, Value);
            transform.position = new Vector3(x, _basePos.y, transform.position.z);
        }
    }
}
