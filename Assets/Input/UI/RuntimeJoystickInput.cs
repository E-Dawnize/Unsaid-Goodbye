using Input.Manager;
using UnityEngine;

namespace Input.UI
{
    public class RuntimeJoystickInput : MonoBehaviour
    {
        [SerializeField] private Joystick _joystick;

        public void Initialize(Joystick joystick)
        {
            _joystick = joystick;
        }

        private void Update()
        {
            VirtualJoystickInput.SetDirection(_joystick != null ? _joystick.Direction : Vector2.zero);
        }

        private void OnDisable()
        {
            VirtualJoystickInput.SetDirection(Vector2.zero);
        }
    }
}
