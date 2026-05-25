using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Input.UI
{
    public static class RuntimeJoystickInstaller
    {
        private const string RootName = "RuntimeJoystickCanvas";
        private const string JoystickPrefabPath = "Joystick Pack/Prefabs/Fixed Joystick";

        public static void Ensure()
        {
            if (!Application.isPlaying)
                return;

            // 只在移动端创建摇杆
            if (!Application.isMobilePlatform)
                return;

            EnsureEventSystem();

            var existing = Object.FindObjectOfType<RuntimeJoystickInput>(true);
            if (!ShouldShowInActiveScene())
            {
                if (existing != null)
                    existing.gameObject.SetActive(false);

                global::Input.Manager.VirtualJoystickInput.SetDirection(Vector2.zero);
                return;
            }

            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            var joystickPrefab = Resources.Load<GameObject>(JoystickPrefabPath);
            if (joystickPrefab == null)
            {
                Debug.LogWarning($"[Joystick] Missing Resources prefab: {JoystickPrefabPath}");
                return;
            }

            var canvasObject = new GameObject(RootName);
            Object.DontDestroyOnLoad(canvasObject);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            var joystickObject = Object.Instantiate(joystickPrefab, canvasObject.transform);
            joystickObject.name = "MobileMoveJoystick";

            if (joystickObject.transform is RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(210f, 190f);
                rect.sizeDelta = new Vector2(230f, 230f);
            }

            var runtimeInput = canvasObject.AddComponent<RuntimeJoystickInput>();
            var joystick = joystickObject.GetComponent<Joystick>();
            runtimeInput.Initialize(joystick);
        }

        private static bool ShouldShowInActiveScene()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            return sceneName != "Start"
                && sceneName != "Start New"
                && sceneName != "SampleScene";
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindObjectOfType<EventSystem>(true);
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem");
                Object.DontDestroyOnLoad(eventSystemObject);
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            if (eventSystem.TryGetComponent<StandaloneInputModule>(out var oldInputModule))
            {
                oldInputModule.enabled = false;
                Object.Destroy(oldInputModule);
            }

            if (!eventSystem.TryGetComponent<InputSystemUIInputModule>(out _))
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }
}
