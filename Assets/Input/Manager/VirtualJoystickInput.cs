using UnityEngine;

namespace Input.Manager
{
    public static class VirtualJoystickInput
    {
        public static Vector2 Direction { get; private set; }

        public static void SetDirection(Vector2 direction)
        {
            Direction = Vector2.ClampMagnitude(direction, 1f);
        }
    }
}
